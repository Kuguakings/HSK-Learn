using System;
using System.Collections.Generic;

[Serializable]
public class AnnouncementData
{
    public string _id;
    public string title;
    public string content;
    public string authorUid;
    public string authorName;
    public long updatedAt;
    public List<string> tags;
    public int viewCount;
    public bool isPublished;

    public AnnouncementData() { tags = new List<string>(); }
}

[Serializable]
public class AnnouncementDraft
{
    public string _id;
    public string title;
    public string content;
    public long savedAt;
}

[Serializable]
public class AnnouncementComment
{
    public string _id;
    public string announcementId;
    public string userUid;
    public string userNickname;
    public string content;
    public long createdAt;

    // 【新增】修改信息：如果是管理员修改，存 "AdminName"，否则为空
    public string modifiedInfo;
}

[Serializable]
public class AnnouncementListWrapper
{
    public List<AnnouncementData> list;
}