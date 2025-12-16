// Ensure helper exists before merge
if (
  typeof window !== "undefined" &&
  typeof window.CleanExpiredGuests !== "function"
) {
  // Lightweight no-op cleanup to avoid breaking guest creation flow
  // In case you later want real cleanup, replace with DB query/removal logic.
  window.CleanExpiredGuests = function () {
    try {
      // Optional: place holder log; keep silent in production if preferred
      if (typeof console !== "undefined") {
        console.log("[TcbWebGL] CleanExpiredGuests noop");
      }
    } catch (e) {}
    return Promise.resolve();
  };
}

mergeInto(LibraryManager.library, {
  // --------------------------------------------------------------
  // 1. 基础认证 (Auth) - 【已修复：使用 window.tcbAuth】
  // --------------------------------------------------------------

  JsRegisterUser: function (
    usernamePtr,
    passwordPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var username = UTF8ToString(usernamePtr);
      var password = UTF8ToString(passwordPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      console.log("[TcbWebGL] Registering: " + username);

      if (!window.tcbAuth || !window.tcbDb) {
        throw new Error("TCB SDK not initialized (window.tcbAuth is missing)");
      }

      window.tcbAuth
        .signInAnonymously()
        .then(function () {
          return window.tcbDb
            .collection("registered_accounts")
            .doc(username)
            .get();
        })
        .then(function (res) {
          if (res.data.length > 0) {
            throw new Error("用户名已存在，请换一个");
          }
          return window.tcbDb
            .collection("registered_accounts")
            .doc(username)
            .set({
              password: password,
              created_at: new Date(),
            });
        })
        .then(function () {
          return window.tcbDb.collection("users").doc(username).set({
            nickname: username,
            password: password,
            created_at: new Date(),
          });
        })
        .then(function () {
          SendMessage(objectName, callbackSuccess, username);
        })
        .catch(function (error) {
          console.error("[TcbWebGL] Register error:", error);
          SendMessage(
            objectName,
            callbackError,
            error.message || "Register Failed"
          );
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsLoginUser: function (
    usernamePtr,
    passwordPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var username = UTF8ToString(usernamePtr);
      var password = UTF8ToString(passwordPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      console.log("[TcbWebGL] Logging in: " + username);

      if (!window.tcbAuth || !window.tcbDb) {
        throw new Error("TCB SDK not initialized (window.tcbAuth is missing)");
      }

      window.tcbAuth
        .signInAnonymously()
        .then(function () {
          return window.tcbDb
            .collection("registered_accounts")
            .doc(username)
            .get();
        })
        .then(function (res) {
          if (!res.data || (Array.isArray(res.data) && res.data.length === 0)) {
            throw new Error("账号不存在");
          }
          var userData = Array.isArray(res.data) ? res.data[0] : res.data;
          if (userData.password !== password) {
            throw new Error("密码错误");
          }
          SendMessage(objectName, callbackSuccess, username);
        })
        .catch(function (error) {
          console.error("[TcbWebGL] Login error:", error);
          if (
            error.code === "DOCUMENT_NOT_EXIST" ||
            (error.message && error.message.indexOf("NotExist") !== -1)
          ) {
            SendMessage(objectName, callbackError, "账号不存在");
          } else {
            SendMessage(
              objectName,
              callbackError,
              error.message || "Login Failed"
            );
          }
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsLogoutUser: function () {
    try {
      if (window.tcbAuth) window.tcbAuth.signOut();
    } catch (err) {
      console.error("[TcbWebGL] Logout JS Error:", err);
    }
  },

  // --------------------------------------------------------------
  // 2. 管理员与用户档案
  // --------------------------------------------------------------

  JsCheckAdminStatus: function (
    uidPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var uid = UTF8ToString(uidPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);
      window.tcbDb
        .collection("admins")
        .doc(uid)
        .get()
        .then(function (res) {
          if (res.data && res.data.length > 0) {
            var adminJson = JSON.stringify(res.data[0]);
            SendMessage(objectName, callbackSuccess, adminJson);
          } else {
            SendMessage(objectName, callbackSuccess, "");
          }
        })
        .catch(function (error) {
          SendMessage(objectName, callbackSuccess, "");
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsGetUserProfile: function (
    uidPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var uid = UTF8ToString(uidPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);
      window.tcbDb
        .collection("users")
        .doc(uid)
        .get()
        .then(function (res) {
          if (res.data && res.data.length > 0) {
            var userJson = JSON.stringify(res.data[0]);
            SendMessage(objectName, callbackSuccess, userJson);
          } else {
            SendMessage(objectName, callbackSuccess, "");
          }
        })
        .catch(function (error) {
          SendMessage(objectName, callbackError, error.message);
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsCreateUserProfile: function (
    uidPtr,
    nicknamePtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var uid = UTF8ToString(uidPtr);
      var nickname = UTF8ToString(nicknamePtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);
      var newProfile = {
        nickname: nickname,
        created_at: new Date(),
      };
      window.tcbDb
        .collection("users")
        .doc(uid)
        .set(newProfile)
        .then(function () {
          SendMessage(objectName, callbackSuccess, "Success");
        })
        .catch(function (error) {
          SendMessage(objectName, callbackError, error.message);
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsUpdateUsername: function (
    uidPtr,
    newNamePtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var uid = UTF8ToString(uidPtr);
      var newName = UTF8ToString(newNamePtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);
      window.tcbDb
        .collection("users")
        .doc(uid)
        .update({
          nickname: newName,
        })
        .then(function () {
          SendMessage(objectName, callbackSuccess, "Success");
        })
        .catch(function (error) {
          SendMessage(objectName, callbackError, error.message);
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  // --------------------------------------------------------------
  // 3. 关卡数据
  // --------------------------------------------------------------

  JsGetLevels: function (objectNamePtr, callbackSuccessPtr, callbackErrorPtr) {
    try {
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      window.tcbDb
        .collection("levels")
        .limit(1000)
        .get()
        .then(function (res) {
          SendMessage(
            objectName,
            callbackSuccess,
            JSON.stringify({ levels: res.data })
          );
        })
        .catch(function (error) {
          SendMessage(objectName, callbackError, error.message);
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsUploadNewLevel: function (
    docIdPtr,
    dataJsonPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var docId = UTF8ToString(docIdPtr);
      var dataJson = UTF8ToString(dataJsonPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      var levelData = JSON.parse(dataJson);
      window.tcbDb
        .collection("levels")
        .doc(docId)
        .set(levelData)
        .then(function () {
          SendMessage(objectName, callbackSuccess, "Upload successful");
        })
        .catch(function (error) {
          SendMessage(objectName, callbackError, error.message);
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  // --------------------------------------------------------------
  // 4. 辅助功能 (Native Prompt) - 【核心修复：使用原生弹窗】
  // --------------------------------------------------------------
  JsShowNativePrompt: function (
    existingTextPtr,
    objectNamePtr,
    callbackSuccessPtr
  ) {
    var existingText = UTF8ToString(existingTextPtr);
    var objectName = UTF8ToString(objectNamePtr);
    var callbackSuccess = UTF8ToString(callbackSuccessPtr);

    // 直接调用浏览器原生 prompt，Unity 无法拦截
    // 缺点是丑，优点是绝对能输入任何字符（数字、大写、中文）
    var input = prompt("Please Input / 请输入:", existingText);

    // 如果用户点了“确定”
    if (input !== null) {
      SendMessage(objectName, callbackSuccess, input);
    }
  },

  // --------------------------------------------------------------
  // 5. 通用数据库 (Generic DB)
  // --------------------------------------------------------------

  JsDbGetCollection: function (
    collectionNamePtr,
    reqIdPtr,
    objectNamePtr,
    successMethodPtr,
    errorMethodPtr
  ) {
    var coll = UTF8ToString(collectionNamePtr);
    var reqId = UTF8ToString(reqIdPtr);
    var obj = UTF8ToString(objectNamePtr);
    var successMethod = UTF8ToString(successMethodPtr);
    var errorMethod = UTF8ToString(errorMethodPtr);
    window.tcbDb
      .collection(coll)
      .get()
      .then(function (res) {
        var msg = reqId + "|" + JSON.stringify(res.data);
        SendMessage(obj, successMethod, msg);
      })
      .catch(function (err) {
        var msg = reqId + "|" + (err.message || "Unknown Error");
        SendMessage(obj, errorMethod, msg);
      });
  },

  JsDbSetDocument: function (
    collPtr,
    docIdPtr,
    jsonPtr,
    reqIdPtr,
    objPtr,
    successPtr,
    errorPtr
  ) {
    var coll = UTF8ToString(collPtr);
    var docId = UTF8ToString(docIdPtr);
    var data = JSON.parse(UTF8ToString(jsonPtr));
    var reqId = UTF8ToString(reqIdPtr);
    var obj = UTF8ToString(objPtr);
    var success = UTF8ToString(successPtr);
    var error = UTF8ToString(errorPtr);

    window.tcbDb
      .collection(coll)
      .doc(docId)
      .set(data)
      .then(function () {
        SendMessage(obj, success, reqId + "|Success");
      })
      .catch(function (err) {
        SendMessage(obj, error, reqId + "|" + err.message);
      });
  },

  JsDbAddDocument: function (
    collPtr,
    jsonPtr,
    reqIdPtr,
    objPtr,
    successPtr,
    errorPtr
  ) {
    var coll = UTF8ToString(collPtr);
    var data = JSON.parse(UTF8ToString(jsonPtr));
    var reqId = UTF8ToString(reqIdPtr);
    var obj = UTF8ToString(objPtr);
    var success = UTF8ToString(successPtr);
    var error = UTF8ToString(errorPtr);
    window.tcbDb
      .collection(coll)
      .add(data)
      .then(function (res) {
        SendMessage(obj, success, reqId + "|" + res.id);
      })
      .catch(function (err) {
        SendMessage(obj, error, reqId + "|" + err.message);
      });
  },

  JsDbDeleteDocument: function (
    collPtr,
    docIdPtr,
    reqIdPtr,
    objPtr,
    successPtr,
    errorPtr
  ) {
    var coll = UTF8ToString(collPtr);
    var docId = UTF8ToString(docIdPtr);
    var reqId = UTF8ToString(reqIdPtr);
    var obj = UTF8ToString(objPtr);
    var success = UTF8ToString(successPtr);
    var error = UTF8ToString(errorPtr);
    window.tcbDb
      .collection(coll)
      .doc(docId)
      .remove()
      .then(function () {
        SendMessage(obj, success, reqId + "|Success");
      })
      .catch(function (err) {
        SendMessage(obj, error, reqId + "|" + err.message);
      });
  },

  JsDbGetDocument: function (
    collPtr,
    docIdPtr,
    reqIdPtr,
    objPtr,
    successPtr,
    errorPtr
  ) {
    var coll = UTF8ToString(collPtr);
    var docId = UTF8ToString(docIdPtr);
    var reqId = UTF8ToString(reqIdPtr);
    var obj = UTF8ToString(objPtr);
    var success = UTF8ToString(successPtr);
    var error = UTF8ToString(errorPtr);
    window.tcbDb
      .collection(coll)
      .doc(docId)
      .get()
      .then(function (res) {
        var json =
          res.data && res.data.length > 0 ? JSON.stringify(res.data[0]) : "";
        SendMessage(obj, success, reqId + "|" + json);
      })
      .catch(function (err) {
        SendMessage(obj, error, reqId + "|" + err.message);
      });
  },

  // --------------------------------------------------------------
  // 6. 游客账号管理
  // --------------------------------------------------------------

  JsCreateGuestAccount: function (
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      console.log("[TcbWebGL] Creating guest account...");

      if (!window.tcbAuth || !window.tcbDb) {
        throw new Error("TCB SDK not initialized");
      }

      var newGuestUid = "";

      // 1. 先匿名登录
      window.tcbAuth
        .signInAnonymously()
        .then(function () {
          // 2. 清理过期游客（3天未转正）
          return window.CleanExpiredGuests();
        })
        .then(function () {
          // 3. 查询所有以 "Guest" 开头的账号，找出最大编号
          return window.tcbDb
            .collection("registered_accounts")
            .where({
              _id: window.tcbDb.RegExp({
                regexp: "^Guest",
                options: "i",
              }),
            })
            .get();
        })
        .then(function (res) {
          // 3. 计算下一个Guest编号
          var maxNum = 0;
          if (res.data && res.data.length > 0) {
            res.data.forEach(function (doc) {
              var match = doc._id.match(/^Guest(\d+)$/i);
              if (match) {
                var num = parseInt(match[1]);
                if (num > maxNum) maxNum = num;
              }
            });
          }
          newGuestUid = "Guest" + (maxNum + 1);
          console.log("[TcbWebGL] Generated guest UID: " + newGuestUid);

          // 4. 在 registered_accounts 中创建游客账号
          return window.tcbDb
            .collection("registered_accounts")
            .doc(newGuestUid)
            .set({
              password: "12345678",
              created_at: new Date(),
              userLevel: -1, // 游客等级
            });
        })
        .then(function () {
          // 5. 在 users 中创建用户档案
          return window.tcbDb.collection("users").doc(newGuestUid).set({
            nickname: newGuestUid, // 昵称就是Guest1, Guest2...
            role: "游客",
            userLevel: -1,
            password: "12345678",
            created_at: new Date(),
          });
        })
        .then(function () {
          console.log(
            "[TcbWebGL] Guest account created successfully: " + newGuestUid
          );
          SendMessage(objectName, callbackSuccess, newGuestUid);
        })
        .catch(function (error) {
          console.error("[TcbWebGL] Guest account creation error:", error);
          SendMessage(
            objectName,
            callbackError,
            error.message || "Failed to create guest account"
          );
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  JsUpgradeGuestAccount: function (
    oldUidPtr,
    newUidPtr,
    newPasswordPtr,
    objectNamePtr,
    callbackSuccessPtr,
    callbackErrorPtr
  ) {
    try {
      var oldUid = UTF8ToString(oldUidPtr);
      var newUid = UTF8ToString(newUidPtr);
      var newPassword = UTF8ToString(newPasswordPtr);
      var objectName = UTF8ToString(objectNamePtr);
      var callbackSuccess = UTF8ToString(callbackSuccessPtr);
      var callbackError = UTF8ToString(callbackErrorPtr);

      console.log(
        "[TcbWebGL] Upgrading guest account from " + oldUid + " to " + newUid
      );

      if (!window.tcbAuth || !window.tcbDb) {
        throw new Error("TCB SDK not initialized");
      }

      var oldUserData = null;

      // 1. 先检查新账号名是否已被占用
      window.tcbAuth
        .signInAnonymously()
        .then(function () {
          return window.tcbDb
            .collection("registered_accounts")
            .doc(newUid)
            .get();
        })
        .then(function (res) {
          if (res.data && res.data.length > 0) {
            throw new Error("账号名已被占用，请换一个");
          }
          // 2. 获取旧用户数据
          return window.tcbDb.collection("users").doc(oldUid).get();
        })
        .then(function (res) {
          if (res.data && res.data.length > 0) {
            oldUserData = res.data[0];
          }
          // 3. 创建新账号记录
          return window.tcbDb
            .collection("registered_accounts")
            .doc(newUid)
            .set({
              password: newPassword,
              created_at: new Date(),
              userLevel: 0, // 转正后变为学员
            });
        })
        .then(function () {
          // 4. 创建新用户档案
          return window.tcbDb.collection("users").doc(newUid).set({
            nickname: newUid, // 默认昵称为账号名
            role: "学员",
            userLevel: 0,
            password: newPassword,
            created_at: new Date(),
          });
        })
        .then(function () {
          // 5. 删除旧游客账号记录
          return window.tcbDb
            .collection("registered_accounts")
            .doc(oldUid)
            .remove();
        })
        .then(function () {
          // 6. 删除旧用户档案
          return window.tcbDb.collection("users").doc(oldUid).remove();
        })
        .then(function () {
          console.log(
            "[TcbWebGL] Guest account upgraded successfully to: " + newUid
          );
          SendMessage(objectName, callbackSuccess, newUid);
        })
        .catch(function (error) {
          console.error("[TcbWebGL] Guest upgrade error:", error);
          SendMessage(
            objectName,
            callbackError,
            error.message || "Failed to upgrade guest account"
          );
        });
    } catch (err) {
      SendMessage(objectName, callbackError, "JS exception: " + err.message);
    }
  },

  // --------------------------------------------------------------
  // 7. 工具函数
  // --------------------------------------------------------------

  JsReloadPage: function () {
    console.log("[TcbWebGL] Reloading page...");
    window.location.reload();
  },
});
