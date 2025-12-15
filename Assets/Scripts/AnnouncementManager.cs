using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Runtime.InteropServices;

public class AnnouncementManager : MonoBehaviour
{
    public static AnnouncementManager instance;

    [Header("UI 面板引用")]
    public GameObject announcementButton;
    public GameObject redDot;
    public GameObject mainPanel;
    public GameObject editorPanel;
    public GameObject detailPanel;
    public GameObject commentOptionPanel;

    [Header("列表 UI")]
    public Transform listContainer;
    public GameObject itemPrefab;
    public Button createButton;
    public Button mainListCloseButton;

    [Header("编辑 UI")]
    public TMP_InputField titleInput;
    public TMP_InputField tagInput;
    public TextMeshProUGUI editorNameText;
    public TextMeshProUGUI editorTimeText;
    public TextMeshProUGUI contentPreviewText;
    public Button openJsInputButton;
    public Button saveDraftButton;
    public Button publishButton;
    public Button deleteButton;
    public TextMeshProUGUI warningText;
    public Button closeEditorButton;

    [Header("详情 UI")]
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailInfo;
    public TextMeshProUGUI detailContent;
    public Transform commentContainer;
    public GameObject commentPrefab;
    public TMP_InputField commentInput;
    public Button postCommentButton;
    public Button closeDetailButton;

    [Header("评论操作 UI")]
    public TextMeshProUGUI commentOptionInfoText;
    public Button btnOptModify;
    public Button btnOptDelete;
    public Button btnOptBack;

    [Header("配置")]
    public float autoPopupIntervalHours = 12f;
    public float fadeDuration = 0.6f;
    private const string PREF_LAST_READ_TIME = "Announce_LastReadTime";

    private CanvasGroup mainPanelCG;
    private CanvasGroup editorPanelCG;
    private CanvasGroup detailPanelCG;
    private CanvasGroup commentOptionCG;

    private AnnouncementData currentEditingData;
    private AnnouncementComment currentSelectedComment;
    private string tempContentCache = "";
    private Coroutine currentWarningRoutine;

    // 【核心修复】改为引用 Native Prompt
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    void Awake()
    {
        instance = this;
        mainPanelCG = SetupCanvasGroup(mainPanel);
        editorPanelCG = SetupCanvasGroup(editorPanel);
        detailPanelCG = SetupCanvasGroup(detailPanel);
        commentOptionCG = SetupCanvasGroup(commentOptionPanel);

        HidePanelImmediate(mainPanelCG);
        HidePanelImmediate(editorPanelCG);
        HidePanelImmediate(detailPanelCG);
        HidePanelImmediate(commentOptionCG);

        if (warningText) warningText.gameObject.SetActive(false);
    }

    private CanvasGroup SetupCanvasGroup(GameObject panel)
    {
        if (panel == null) return null;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    private void HidePanelImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    void Start()
    {
        if (announcementButton) announcementButton.GetComponent<Button>().onClick.AddListener(OpenMainPanel);
        if (createButton) createButton.onClick.AddListener(OnCreateNewClick);
        if (mainListCloseButton) mainListCloseButton.onClick.AddListener(() => ClosePanel(mainPanelCG));

        if (openJsInputButton) openJsInputButton.onClick.AddListener(OpenJsInput);
        if (saveDraftButton) saveDraftButton.onClick.AddListener(SaveDraft);
        if (publishButton) publishButton.onClick.AddListener(PublishAnnouncement);
        if (deleteButton) deleteButton.onClick.AddListener(DeleteAnnouncement);

        if (closeEditorButton) closeEditorButton.onClick.AddListener(() => {
            StartCoroutine(TransitionTo(editorPanelCG, mainPanelCG));
            RefreshList();
        });

        if (postCommentButton) postCommentButton.onClick.AddListener(PostComment);

        if (closeDetailButton) closeDetailButton.onClick.AddListener(() => {
            StartCoroutine(TransitionTo(detailPanelCG, mainPanelCG));
        });

        if (btnOptBack) btnOptBack.onClick.AddListener(() => ClosePanel(commentOptionCG));
        if (btnOptDelete) btnOptDelete.onClick.AddListener(DeleteSelectedComment);
        if (btnOptModify) btnOptModify.onClick.AddListener(OpenModifyCommentInput);

        StartCoroutine(CheckAutoPopupRoutine());
    }

    IEnumerator CheckAutoPopupRoutine()
    {
        while (!TcbManager.isLoggedIn) yield return null;
        if (createButton) createButton.gameObject.SetActive(TcbManager.IsAdmin);

        string lastReadStr = PlayerPrefs.GetString(PREF_LAST_READ_TIME, "0");
        long lastReadTime = long.Parse(lastReadStr);
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if ((currentTime - lastReadTime) > autoPopupIntervalHours * 3600)
        {
            if (redDot) redDot.SetActive(true);
        }
    }

    private void PostComment()
    {
        string content = commentInput.text;
        if (string.IsNullOrEmpty(content)) return;

        Debug.Log("正在发送评论: " + content);

        AnnouncementComment newComment = new AnnouncementComment
        {
            _id = Guid.NewGuid().ToString(),
            announcementId = currentEditingData._id,
            userUid = TcbManager.CurrentUid,
            userNickname = TcbManager.CurrentNickname,
            content = content,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        TcbManager.instance.AddDocument("announcement_comments", newComment, () => {
            Debug.Log("评论发送成功！");
            commentInput.text = "";
            RefreshComments(currentEditingData._id);
        }, (err) => Debug.LogError("评论发送失败: " + err));
    }

    private void OnCommentRightClicked(AnnouncementComment comment)
    {
        currentSelectedComment = comment;
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(comment.createdAt).LocalDateTime;
        if (commentOptionInfoText)
            commentOptionInfoText.text = $"{comment.userNickname}\n发布于 {dt.ToString("MM.dd HH:mm")}";
        ShowPanel(commentOptionCG);
    }

    private void DeleteSelectedComment()
    {
        if (currentSelectedComment == null) return;
        bool isMine = currentSelectedComment.userUid == TcbManager.CurrentUid;
        bool isAdmin = TcbManager.IsAdmin;

        if (!isMine && !isAdmin)
        {
            Debug.LogWarning("无权删除他人评论");
            return;
        }

        TcbManager.instance.DeleteDocument("announcement_comments", currentSelectedComment._id, () => {
            Debug.Log("评论删除成功");
            ClosePanel(commentOptionCG);
            RefreshComments(currentEditingData._id);
        }, (err) => Debug.LogError("删除评论失败: " + err));
    }

    private void OpenModifyCommentInput()
    {
        if (currentSelectedComment == null) return;
        bool isMine = currentSelectedComment.userUid == TcbManager.CurrentUid;
        bool isAdmin = TcbManager.IsAdmin;
        if (!isMine && !isAdmin) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        // 【修复】使用 Native Prompt
        JsShowNativePrompt(currentSelectedComment.content, gameObject.name, "OnModifyCommentSuccess");
#else
        OnModifyCommentSuccess(currentSelectedComment.content + " [修改]");
#endif
    }

    public void OnModifyCommentSuccess(string newContent)
    {
        if (currentSelectedComment == null) return;
        currentSelectedComment.content = newContent;
        bool isMine = currentSelectedComment.userUid == TcbManager.CurrentUid;
        if (!isMine && TcbManager.IsAdmin)
            currentSelectedComment.modifiedInfo = TcbManager.CurrentNickname;
        else
            currentSelectedComment.modifiedInfo = "";

        TcbManager.instance.SetDocument("announcement_comments", currentSelectedComment._id, currentSelectedComment, () => {
            Debug.Log("评论修改成功");
            ClosePanel(commentOptionCG);
            RefreshComments(currentEditingData._id);
        }, (err) => Debug.LogError("修改评论失败: " + err));
    }

    private void RefreshComments(string announcementId)
    {
        foreach (Transform child in commentContainer) Destroy(child.gameObject);
        TcbManager.instance.GetCollectionData<AnnouncementComment>("announcement_comments", (allComments) => {
            if (allComments == null) return;
            var myComments = allComments.FindAll(c => c.announcementId == announcementId);
            myComments.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
            foreach (var comment in myComments)
            {
                GameObject item = Instantiate(commentPrefab, commentContainer);
                CommentItem script = item.GetComponent<CommentItem>();
                if (script) script.Setup(comment, OnCommentRightClicked);
            }
        }, (err) => Debug.LogWarning("Refresh Comments Failed: " + err));
    }

    private void ShowPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(cg, 0f, 1f));
    }

    private void ClosePanel(CanvasGroup cg)
    {
        if (cg == null) return;
        StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, () => cg.gameObject.SetActive(false)));
    }

    private IEnumerator TransitionTo(CanvasGroup from, CanvasGroup to)
    {
        if (from != null && from.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(from, 1f, 0f));
            from.gameObject.SetActive(false);
        }
        if (to != null)
        {
            to.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(to, 0f, 1f));
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, Action onComplete = null)
    {
        float timer = 0f;
        cg.alpha = start;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
        bool isVisible = end > 0.01f;
        cg.interactable = isVisible;
        cg.blocksRaycasts = isVisible;
        onComplete?.Invoke();
    }

    public void OpenMainPanel()
    {
        ShowPanel(mainPanelCG);
        RefreshList();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(PREF_LAST_READ_TIME, now.ToString());
        if (redDot) redDot.SetActive(false);
    }

    public void RefreshList()
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);
        TcbManager.instance.GetCollectionData<AnnouncementData>("announcements", (list) => {
            if (list == null) return;
            list.Sort((a, b) => b.updatedAt.CompareTo(a.updatedAt));
            foreach (var data in list)
            {
                GameObject item = Instantiate(itemPrefab, listContainer);
                AnnouncementItem script = item.GetComponent<AnnouncementItem>();
                if (script) script.Setup(data, OnItemViewClicked, OnItemEditClicked);
            }
        }, (err) => Debug.LogError(err));
    }

    private void OnItemViewClicked(AnnouncementData data)
    {
        StartCoroutine(TransitionTo(mainPanelCG, detailPanelCG));
        currentEditingData = data;
        detailTitle.text = data.title;
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(data.updatedAt).LocalDateTime;
        detailInfo.text = $"作者: {data.authorName}   时间: {dt.ToString("yyyy/MM/dd HH:mm")}";
        detailContent.text = data.content;
        RefreshComments(data._id);
    }

    private void OnItemEditClicked(AnnouncementData data)
    {
        if (!TcbManager.IsAdmin) return;
        currentEditingData = data;
        StartCoroutine(TransitionTo(mainPanelCG, editorPanelCG));
        ShowEditorUI(false);
    }

    private void DeleteAnnouncement()
    {
        bool isSuperAdmin = TcbManager.AdminLevel >= 999;
        bool isDraft = !currentEditingData.isPublished;
        if (!isDraft && !isSuperAdmin)
        {
            if (currentWarningRoutine != null) StopCoroutine(currentWarningRoutine);
            currentWarningRoutine = StartCoroutine(ShowWarningAnim("权限不足：请联系超级管理员进行删除"));
            return;
        }
        if (string.IsNullOrEmpty(currentEditingData._id))
        {
            StartCoroutine(TransitionTo(editorPanelCG, mainPanelCG));
            return;
        }
        string collectionName = currentEditingData.isPublished ? "announcements" : "announcement_drafts";
        string docId = currentEditingData.isPublished ? currentEditingData._id : TcbManager.CurrentUid;
        TcbManager.instance.DeleteDocument(collectionName, docId, () => {
            StartCoroutine(TransitionTo(editorPanelCG, mainPanelCG));
            RefreshList();
        }, (err) => Debug.LogError(err));
    }

    IEnumerator ShowWarningAnim(string message)
    {
        if (warningText == null) yield break;
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningText.color = Color.red;
        yield return null;
        float timer = 0f;
        while (timer < 3.5f)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) break;
            timer += Time.deltaTime;
            yield return null;
        }
        warningText.gameObject.SetActive(false);
        currentWarningRoutine = null;
    }

    private void OnCreateNewClick()
    {
        string myUid = TcbManager.CurrentUid;
        TcbManager.instance.GetDocument<AnnouncementDraft>("announcement_drafts", myUid, (draft) => {
            if (draft != null && !string.IsNullOrEmpty(draft.title))
            {
                currentEditingData = new AnnouncementData { title = draft.title, content = draft.content, authorName = TcbManager.CurrentNickname, authorUid = myUid };
            }
            else
            {
                currentEditingData = new AnnouncementData { authorName = TcbManager.CurrentNickname, authorUid = myUid };
            }
            StartCoroutine(TransitionTo(mainPanelCG, editorPanelCG));
            ShowEditorUI(false);
        }, (err) => {
            currentEditingData = new AnnouncementData { authorName = TcbManager.CurrentNickname, authorUid = myUid };
            StartCoroutine(TransitionTo(mainPanelCG, editorPanelCG));
            ShowEditorUI(false);
        });
    }

    private void ShowEditorUI(bool forceActive = true)
    {
        if (forceActive) editorPanel.SetActive(true);
        if (warningText) warningText.gameObject.SetActive(false);
        titleInput.text = currentEditingData.title;
        tempContentCache = currentEditingData.content;
        contentPreviewText.text = tempContentCache;
        if (currentEditingData.tags != null && currentEditingData.tags.Count > 0) tagInput.text = currentEditingData.tags[0];
        else tagInput.text = "";
        StartCoroutine(UpdateTimeRoutine());
    }

    IEnumerator UpdateTimeRoutine()
    {
        while (editorPanel.activeSelf)
        {
            if (editorNameText) editorNameText.text = "编辑: " + TcbManager.CurrentNickname;
            if (editorTimeText) editorTimeText.text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            yield return new WaitForSeconds(1f);
        }
    }

    private void OpenJsInput()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // 【修复】使用 Native Prompt
        JsShowNativePrompt(tempContentCache, gameObject.name, "OnJsInputSuccess");
#else
        OnJsInputSuccess(tempContentCache + " (Simulated)");
#endif
    }

    public void OnJsInputSuccess(string text)
    {
        tempContentCache = text;
        currentEditingData.content = text;
        contentPreviewText.text = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
    }
    // OnJsInputError 不再需要，但如果其他地方还在引用，可以留个空函数，或者直接删掉。这里删掉了。

    private void SaveDraft()
    {
        currentEditingData.title = titleInput.text;
        currentEditingData.content = tempContentCache;
        AnnouncementDraft draft = new AnnouncementDraft { title = currentEditingData.title, content = currentEditingData.content, savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        TcbManager.instance.SetDocument("announcement_drafts", TcbManager.CurrentUid, draft, () => Debug.Log("Draft Saved"), (err) => Debug.LogError(err));
    }

    private void PublishAnnouncement()
    {
        currentEditingData.title = titleInput.text;
        currentEditingData.content = tempContentCache;
        currentEditingData.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        currentEditingData.authorName = TcbManager.CurrentNickname;
        currentEditingData.isPublished = true;
        currentEditingData.tags = new List<string>();
        if (!string.IsNullOrEmpty(tagInput.text)) currentEditingData.tags.Add(tagInput.text);

        if (string.IsNullOrEmpty(currentEditingData._id))
        {
            TcbManager.instance.AddDocument("announcements", currentEditingData, () => {
                TcbManager.instance.DeleteDocument("announcement_drafts", TcbManager.CurrentUid);
                StartCoroutine(TransitionTo(editorPanelCG, mainPanelCG));
                OpenMainPanel();
            }, (err) => Debug.LogError(err));
        }
        else
        {
            TcbManager.instance.SetDocument("announcements", currentEditingData._id, currentEditingData, () => {
                TcbManager.instance.DeleteDocument("announcement_drafts", TcbManager.CurrentUid);
                StartCoroutine(TransitionTo(editorPanelCG, mainPanelCG));
                OpenMainPanel();
            }, (err) => Debug.LogError(err));
        }
    }
}