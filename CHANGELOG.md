# Changelog

本项目的所有显著变更都会记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 新增
- 初始项目结构（Avalonia 12 + FluentAvalonia 3）
- 罗马字 → 平假名 转换器
- 文件扫描、预览、批量重命名功能
- 主题跟随系统（亮/暗模式 + 系统强调色）
- GitHub 构建（`.github/workflows/build.yml`）与自动发布（`.github/workflows/release.yml`）
- 贡献指南与行为准则

## [0.1.0] - 2026-08-24

### 新增
- 首个版本。

### 特性
- 批量扫描文件夹并预览「原文件名 → 转换后文件名」。
- 仅转换文件名主体，扩展名保持原样，未识别字符原样保留。
- 支持全选/全不选，仅处理勾选且状态为「待转换」的文件。
- 自动跳过目标已存在、源与目标相同等情况。
- 状态统计与操作日志。
- 主题色跟随 Windows 系统（亮/暗模式与强调色）。
- 发布产物自动打包为 zip（`RomaToHiragana-win-x64-<版本>.zip`）。
