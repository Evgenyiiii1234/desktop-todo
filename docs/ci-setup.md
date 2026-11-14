# 自动构建（GitHub Actions）

仓库里的 `.github/workflows/build.yml` 需要在推送时具备 `workflow` 权限的 token。
如果你是用网页上传，把本文件改名为 `build.yml` 放到 `.github/workflows/` 下即可。

## 用网页添加
1. 打开仓库 → Code → Add file → Create new file
2. 文件名填 `.github/workflows/build.yml`
3. 把本文件内容整段粘进去 → Commit

## 用命令行添加
需要带 `workflow` scope 的 Personal Access Token：

```powershell
copy-item docs\github-actions.yml.example .github\workflows\build.yml
git add .github/workflows/build.yml
git commit -m "ci: 添加自动构建"
git push
```

推送 tag（`git push origin v0.x.y`）时会自动编译并把 exe 挂到 Release。