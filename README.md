# Element-Crush Project

## 🎮 Play Online / 在线试玩

Click the button below to play the latest web version:
(点击下方按钮直接开始游戏)

[![Play Now](https://img.shields.io/badge/Play-On_Netlify-00C7B7?style=for-the-badge&logo=netlify&logoColor=white)](https://hsklearningv1-3-1.netlify.app/)

> Server: Netlify | Database: Tencent Cloud
> (游戏服务器：Netlify | 数据库：腾讯云)

---

# 🛠️ Developer Setup Guide

Hi! Since you are already familiar with GitHub, I'll keep this brief. Here are the specific requirements to get this Unity project running correctly on your machine.

### 1. Unity Version Requirement
**⚠️ Important:** Please make sure you are using **Unity [2022.3.10f1]** (or the exact version match).
* *Version mismatches will likely break the scene files.*

### 2. Cloning (LFS Warning)
**⛔ Do NOT use "Download ZIP".**
* This repository relies heavily on **Git LFS** for textures and models.
* Please use `git clone` (via Terminal or GitHub Desktop).
* *Check:* After cloning, if your total folder size is small (MBs instead of GBs), `git lfs pull` might be needed.

### 3. How to Load the Level
When you first open the project in Unity, the viewport will look **empty/blue**. This is normal.

**👉 You must manually load the scene:**
1.  Go to the **Project Panel** at the bottom.
2.  Navigate to: `Assets` -> `Scenes`.
3.  Double-click the main scene file (e.g., `Main` or `SampleScene`).

### 4. Workflow Notes
* **WebGL Build:** The link above runs the WebGL build.
* **Syncing:** Please pull the latest changes before starting your work to avoid binary merge conflicts.

Let's make a great game! 🚀
