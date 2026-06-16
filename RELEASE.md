# Tenlux 2.0.0 Release

## Summary

Tenlux 2.0.0 focuses on three things:

- a cleaner long-term WinUI 3 experience
- a lighter background state when settings are not open
- a more complete public-facing product package

## Product highlights

- tray-first theme switching
- dark/light wallpaper linking
- global hotkeys
- scheduled switching
- configuration import/export
- lightweight settings with no in-app diagnostics or support-pack clutter

## Engineering highlights

- settings-window close path now releases more UI and image resources
- delayed trim and compaction flow for better background behavior
- reusable validation scripts for build, launch assets, and background-state measurement

## Product and launch assets

- platform-specific copy for Bilibili, Douyin, Xiaohongshu, and Xiaoheihe
- promo landing page
- FAQ, comment replies, content calendar, launch checklist, shot list
- launch pack export script

## Remaining work before calling it fully verified

- full window-level visual verification against the current `D:\Codex\src-winui` build
- repeated background-state measurements once the external legacy Tenlux instance is cleared
