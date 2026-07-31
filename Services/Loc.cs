using System.Collections.Generic;

namespace UtilityMaster.Services;

public static class Loc
{
    private static string _lang = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["en"] = new()
        {
            // App
            ["app.title"] = "Utility Master",
            ["window.title"] = "Utility Master",
            ["nades"] = "Nades",
            ["tricks"] = "Tricks",
            ["settings.nav"] = "Settings",
            ["about"] = "About",

            // About
            ["about.subtitle"] = "CS2 Nades & Tricks Tool",
            ["about.links"] = "Links",
            ["about.license"] = "License",
            ["about.license_text"] = "MIT License - Open Source",
            ["about.disclaimers"] = "Disclaimers",
            ["about.credits"] = "Credits",
            ["about.credit1"] = "Map icons & minimap images from ",
            ["about.credit2"] = "Nade icons from ",
            ["about.disclaimer1"] = "Nade icons, map images, and related assets are property of Valve Corporation, used for non-commercial community purposes. Contact primspark@outlook.com for removal requests.",
            ["about.disclaimer2"] = "This is a community-made unofficial tool. No affiliation, association, sponsorship, or endorsement by Valve Corporation.",
            ["about.disclaimer3"] = "CS2 related trademarks are property of Valve Corporation.",
            ["about.disclaimer4"] = "For educational and exchange purposes only.",
            ["about.version"] = "v1.5.0",


            // Settings
            ["settings.title"] = "Settings",
            ["settings.default_filters"] = "Default Filters",
            ["settings.default_type"] = "Default Type",
            ["settings.default_side"] = "Default Side",
            ["settings.default_trick_type"] = "Default Trick Type",
            ["pro"] = "PRO",
            ["add_lineup.pro"] = "PRO (Pro Selected)",
            ["settings.conflict_thresholds"] = "Conflict Distance Thresholds",
            ["settings.nades_target"] = "Nades Target (px)",
            ["settings.nades_lineup"] = "Nades Lineup (px)",
            ["settings.wallbang_target"] = "Wallbang Target (px)",
            ["settings.wallbang_lineup"] = "Wallbang Lineup (px)",
            ["settings.tricks_target"] = "Tricks Target (px)",
            ["settings.display"] = "Display",
            ["settings.auto_play"] = "Auto-play video",
            ["settings.language"] = "Language",
            ["settings.chinese_terms"] = "Chinese map names",
            ["settings.storage"] = "Storage Paths",
            ["settings.data_path"] = "Data path",
            ["settings.screenshot_path"] = "Screenshot path",
            ["settings.save"] = "Save Settings",
            ["settings.saved"] = "Settings saved.",
            ["settings.security"] = "Security",
            ["settings.allow_delete_defaults"] = "Allow deleting default spots",

            // Map
            ["map.add_target"] = "Add Target",
            ["map.add_spot"] = "Add Spot",
            ["map.add_lineup"] = "Add Lineup",
            ["map.edit_target"] = "Edit Target",
            ["map.edit_lineup"] = "Edit Lineup",
            ["map.delete_target"] = "Delete ''{0}''",
            ["map.delete_lineup"] = "Delete Lineup",
            ["map.delete_confirm"] = "Delete ''{0}'' and {1} lineup(s)?",
            ["map.delete_title"] = "Delete Target",
            ["map.protected"] = "Cannot modify default spots.",
            ["map.protected_title"] = "Protected",
            ["map.min_lineup"] = "Target must have at least one lineup.",
            ["map.cannot_delete"] = "Cannot Delete",
            ["map.search"] = "Search...",
            ["map.search_title"] = "Search",
            ["map.search_placeholder"] = "Search spots...",
            ["map.search_no_results"] = "No matches",
            ["map.search_count"] = "{0} results",
            ["map.search_hint"] = "Double-click to locate",
            ["map.search_show"] = "Show search panel",
            ["map.search_collapse"] = "Collapse panel",
            ["map.search_clear"] = "Clear",
            ["map.delete_lineup_confirm"] = "Delete lineup #{0}?",
            ["map.delete_lineup_title"] = "Delete Lineup",
            ["map.delete_last_lineup_confirm"] = "This is the only lineup. Delete the whole target '{0}'?",
            ["map.lineup_variant_title"] = "Lineup Variant",
            ["map.overwrite_lineup"] = "Lineup #{0} is already at this position. Overwrite it and remove this one?",
            ["map.new_variant"] = "A lineup (#{0}) already exists at this position. Create as a new variant (same position, different setup)?",
            ["map.browse_targets"] = "Targets",
            ["map.browse_lineups"] = "Lineups",

            // Lineup detail
            ["lineup.title"] = "Lineup Detail",
            ["lineup.variants"] = "Lineup Variants",
            ["lineup.seq"] = "Seq #{0}",
            ["lineup.target_label"] = "Target: {0}",
            ["lineup.aim"] = "Aim Description",
            ["lineup.throw_type"] = "Throw Type",
            ["lineup.video"] = "Video",
            ["lineup.notes"] = "Notes",
            ["lineup.none"] = "(none)",
            ["lineup.images"] = "Images",

            // Create Target
            ["create_target.title"] = "Create Target",
            ["create_target.title_trick"] = "Create Spot",
            ["create_target.type"] = "Type",
            ["create_target.side"] = "Side",
            ["create_target.x"] = "Target X (pixel)",
            ["create_target.y"] = "Target Y (pixel)",
            ["create_target.btn"] = "Create and Add Lineup",
            ["create_target.btn_trick"] = "Create",
            ["create_target.error_name"] = "Name is required.",
            ["create_target.error_coord"] = "Invalid coordinates.",

            // Add/Edit Lineup
            ["add_lineup.title"] = "Add Lineup",
            ["add_lineup.name"] = "Utility Name",
            ["add_lineup.side"] = "Side",
            ["add_lineup.edit_title"] = "Edit Lineup",
            ["add_lineup.pick_hint"] = "Click on map to set position:",
            ["add_lineup.pick_btn"] = "Click on map to set position:",
            ["add_lineup.x"] = "Position X (pixel)",
            ["add_lineup.y"] = "Position Y (pixel)",
            ["add_lineup.aim"] = "Aim Description",
            ["add_lineup.throw_type"] = "Throw Type",
            ["add_lineup.video"] = "Video URL",
            ["add_lineup.notes"] = "Notes",
            ["add_lineup.images"] = "Images",
            ["add_lineup.btn"] = "Add Lineup",
            ["add_lineup.save_btn"] = "Save",
            ["add_lineup.paste_btn"] = "Paste Image (Ctrl+V)",
            ["add_lineup.paste_failed"] = "Paste failed:",
            ["add_lineup.error_coord"] = "Invalid coordinates.",
            ["add_lineup.error_name"] = "Utility name is required.",
            ["add_lineup.remove_image"] = "Remove this image?",
            ["add_lineup.remove_title"] = "Remove",

            // Trick Detail
            ["trick.title"] = "Trick Detail",
            ["trick.name"] = "Name",
            ["trick.type"] = "Type",
            ["trick.side"] = "Side",
            ["trick.coord"] = "Coordinates",
            ["trick.images"] = "Images",
            ["trick.video"] = "Video",
            ["trick.notes"] = "Notes",
            ["trick.none"] = "(none)",

            // Create Trick
            ["create_trick.title"] = "Create Spot",
            ["create_trick.name"] = "Spot Name",
            ["create_trick.type"] = "Trick Type",
            ["create_trick.side"] = "Side",
            ["create_trick.side_both"] = "Both",
            ["create_trick.x"] = "Position X (pixel)",
            ["create_trick.y"] = "Position Y (pixel)",
            ["create_trick.video"] = "Video URL",
            ["create_trick.notes"] = "Notes",
            ["create_trick.btn"] = "Create Spot",
            ["create_trick.error_name"] = "Name is required.",
            ["create_trick.error_coord"] = "Invalid coordinates.",

            // Types
            ["smoke"] = "Smoke",
            ["flash"] = "Flash",
            ["he"] = "HE",
            ["molotov"] = "Molotov",
            ["incendiary"] = "Incendiary",
            ["wallbang"] = "Wallbang",
            ["boost"] = "Boost",
            ["jump"] = "Jump",
            ["camp"] = "Camp",

            // Throw types
            ["standing"] = "Standing",
            ["crouching"] = "Crouching",
            ["jump_throw"] = "Jump-throw",
            ["running"] = "Running",
            ["run_throw"] = "Run-throw",
            ["run_jump_throw"] = "Run Jump-throw",
            ["crouch_jump_throw"] = "Crouch Jump-throw",

            // Floor
            ["upper"] = "Upper",
            ["lower"] = "Lower",

            // Wallbang/Jump terms
            ["wallbang_target"] = "Wallbang Spot",
            ["wallbang_lineup"] = "Firing Position",
            ["wallbang_add_target"] = "Add Wallbang Spot",
            ["wallbang_add_lineup"] = "Add Firing Position",
            ["jump_target"] = "Jump Spot",
            ["jump_lineup"] = "Jump Position",
            ["jump_add_target"] = "Add Jump Spot",
            ["jump_add_lineup"] = "Add Jump Position",

            // General
            ["edit"] = "Edit",
            ["delete"] = "Delete",
            ["cancel"] = "Cancel",
            ["confirm"] = "Confirm",
            ["close"] = "Close",
            ["yes"] = "Yes",
            ["no"] = "No",
            ["error"] = "Error",
            ["warning"] = "Warning",
            ["info"] = "Info",

            // Home
            ["home.nades"] = "Nades",
            ["home.tricks"] = "Tricks",

            // Trick edit
            ["trick_edit.title"] = "Edit Spot",

            // Nearby target
            ["nearby.title"] = "Nearby Target Found",
            ["nearby.msg"] = "A target ''{0}'' already exists nearby. Add a lineup to it instead?",
        },
        ["zh"] = new()
        {
                ["window.title"] = "Utility Master",
            // App
            ["app.title"] = "Utility Master",
            ["window.title"] = "Utility Master",
            ["nades"] = "道具",
            ["tricks"] = "技巧",
            ["settings.nav"] = "设置",
            ["about"] = "关于",

            // About
            ["about.subtitle"] = "CS2 道具与技巧工具",
            ["about.links"] = "链接",
            ["about.license"] = "许可证",
            ["about.license_text"] = "MIT 许可证 - 开源",
            ["about.disclaimers"] = "声明",
            ["about.disclaimer1"] = "道具图标、地图图像等素材版权归 Valve Corporation 所有，仅用于非商业社区用途。如需移除请联系 primspark@outlook.com。",
            ["about.disclaimer2"] = "本工具为社区制作的非官方工具，与 Valve Corporation 无任何关联、合作、赞助或背书关系。",
            ["about.disclaimer3"] = "CS2 相关商标归 Valve Corporation 所有。",
            ["about.disclaimer4"] = "仅供学习交流使用。",
            ["about.version"] = "v1.5.0",

            // Settings
            ["settings.title"] = "设置",
            ["settings.default_filters"] = "默认筛选",
            ["settings.default_type"] = "默认道具类型",
            ["settings.default_side"] = "默认阵营",
            ["settings.default_trick_type"] = "默认技巧类型",
            ["pro"] = "职业",
            ["add_lineup.pro"] = "职业精选 (PRO)",
            ["settings.conflict_thresholds"] = "冲突距离阈值",
            ["settings.nades_target"] = "道具落点 (px)",
            ["settings.nades_lineup"] = "道具站点 (px)",
            ["settings.wallbang_target"] = "穿点目标 (px)",
            ["settings.wallbang_lineup"] = "穿点站位 (px)",
            ["settings.tricks_target"] = "技巧点位 (px)",
            ["settings.display"] = "显示",
            ["settings.auto_play"] = "自动播放视频",
            ["settings.language"] = "界面语言",
            ["settings.chinese_terms"] = "中文地图名",
            ["settings.storage"] = "存储路径",
            ["settings.data_path"] = "数据路径",
            ["settings.screenshot_path"] = "截图路径",
            ["settings.save"] = "保存设置",
            ["settings.saved"] = "设置已保存。",
            ["settings.security"] = "安全",
            ["settings.allow_delete_defaults"] = "允许删除默认点位",

            // Map
            ["map.add_target"] = "添加落点",
            ["map.add_spot"] = "创建点位",
            ["map.add_lineup"] = "添加站点",
            ["map.edit_target"] = "编辑落点",
            ["map.edit_lineup"] = "编辑站点",
            ["map.delete_target"] = "删除 ''{0}''",
            ["map.delete_lineup"] = "删除站点",
            ["map.delete_confirm"] = "删除 ''{0}'' 及其 {1} 个站点？",
            ["map.delete_title"] = "删除落点",
            ["map.protected"] = "无法修改默认点位。",
            ["map.protected_title"] = "受保护",
            ["map.min_lineup"] = "落点至少需要保留一个站点。",
            ["map.cannot_delete"] = "无法删除",
            ["map.search"] = "搜索...",
            ["map.search_title"] = "搜索",
            ["map.search_placeholder"] = "搜索点位...",
            ["map.search_no_results"] = "没有匹配结果",
            ["map.search_count"] = "{0} 个结果",
            ["map.search_hint"] = "双击定位",
            ["map.search_show"] = "显示搜索面板",
            ["map.search_collapse"] = "收起面板",
            ["map.search_clear"] = "清空",
            ["map.delete_lineup_confirm"] = "删除站位 #{0}？",
            ["map.delete_lineup_title"] = "删除站位",
            ["map.delete_last_lineup_confirm"] = "这是最后一个站位。是否删除整个落点 '{0}'？",
            ["map.lineup_variant_title"] = "站位变体",
            ["map.overwrite_lineup"] = "站位 #{0} 已在此位置。覆盖它并移除当前站位？",
            ["map.new_variant"] = "站位 (#{0}) 已在此位置。是否创建为新变体（同位置不同设置）？",
            ["map.browse_targets"] = "落点",
            ["map.browse_lineups"] = "点位",

            // Lineup detail
            ["lineup.title"] = "站点详情",
            ["lineup.seq"] = "序号 #{0}",
            ["lineup.target_label"] = "落点: {0}",
            ["lineup.aim"] = "瞄准参照描述",
            ["lineup.throw_type"] = "投掷方式",
            ["lineup.video"] = "视频",
            ["lineup.notes"] = "备注",
            ["lineup.none"] = "(无)",
            ["lineup.images"] = "图片",

            // Create Target
            ["create_target.title"] = "创建落点",
            ["create_target.title_trick"] = "创建点位",
            ["create_target.type"] = "道具类型",
            ["create_target.side"] = "阵营",
            ["create_target.x"] = "落点 X (像素)",
            ["create_target.y"] = "落点 Y (像素)",
            ["create_target.btn"] = "创建并添加站点",
            ["create_target.btn_trick"] = "创建",
            ["create_target.error_name"] = "名称为必填项。",
            ["create_target.error_coord"] = "坐标无效。",

            // Add/Edit Lineup
            ["add_lineup.title"] = "添加站点",
            ["add_lineup.name"] = "道具名称",
            ["add_lineup.side"] = "阵营",
            ["add_lineup.edit_title"] = "编辑站点",
            ["add_lineup.pick_hint"] = "点击小地图选择位置：",
            ["add_lineup.pick_btn"] = "点击小地图选择位置：",
            ["add_lineup.x"] = "位置 X (像素)",
            ["add_lineup.y"] = "位置 Y (像素)",
            ["add_lineup.aim"] = "瞄准参照描述",
            ["add_lineup.throw_type"] = "投掷方式",
            ["add_lineup.video"] = "视频链接",
            ["add_lineup.notes"] = "备注",
            ["add_lineup.images"] = "图片",
            ["add_lineup.btn"] = "添加站点",
            ["add_lineup.save_btn"] = "保存",
            ["add_lineup.paste_btn"] = "粘贴图片 (Ctrl+V)",
            ["add_lineup.paste_failed"] = "粘贴失败:",
            ["add_lineup.error_coord"] = "坐标无效。",
            ["add_lineup.error_name"] = "道具名称不能为空。",
            ["add_lineup.remove_image"] = "移除此图片？",
            ["add_lineup.remove_title"] = "移除",

            // Trick Detail
            ["trick.title"] = "技巧详情",
            ["trick.name"] = "名称",
            ["trick.type"] = "类型",
            ["trick.side"] = "阵营",
            ["trick.coord"] = "坐标",
            ["trick.images"] = "图片",
            ["trick.video"] = "视频",
            ["trick.notes"] = "备注",
            ["trick.none"] = "(无)",

            // Create Trick
            ["create_trick.title"] = "创建点位",
            ["create_trick.name"] = "点位名称",
            ["create_trick.type"] = "技巧类型",
            ["create_trick.side"] = "阵营",
            ["create_trick.side_both"] = "双侧",
            ["create_trick.x"] = "位置 X (像素)",
            ["create_trick.y"] = "位置 Y (像素)",
            ["create_trick.video"] = "视频链接",
            ["create_trick.notes"] = "备注",
            ["create_trick.btn"] = "创建点位",
            ["create_trick.error_name"] = "名称为必填项。",
            ["create_trick.error_coord"] = "坐标无效。",

            // Types
            ["smoke"] = "烟雾弹",
            ["flash"] = "闪光弹",
            ["he"] = "手雷",
            ["molotov"] = "燃烧弹",
            ["incendiary"] = "燃烧弹",
            ["wallbang"] = "穿点",
            ["boost"] = "双架",
            ["jump"] = "身法",
            ["camp"] = "架点",

            // Throw types
            ["standing"] = "站投",
            ["crouching"] = "蹲投",
            ["jump_throw"] = "跳投",
            ["running"] = "跑投",
            ["run_throw"] = "跑投",
            ["run_jump_throw"] = "跑跳投",
            ["crouch_jump_throw"] = "蹲跳投",

            // Floor
            ["upper"] = "上层",
            ["lower"] = "下层",

            // Wallbang/Jump terms
            ["wallbang_target"] = "穿点",
            ["wallbang_lineup"] = "站位",
            ["wallbang_add_target"] = "添加穿点",
            ["wallbang_add_lineup"] = "添加站位",
            ["jump_target"] = "身法点",
            ["jump_lineup"] = "身法位置",
            ["jump_add_target"] = "添加身法点",
            ["jump_add_lineup"] = "添加身法位置",

            // General
            ["edit"] = "编辑",
            ["delete"] = "删除",
            ["cancel"] = "取消",
            ["confirm"] = "确认",
            ["close"] = "关闭",
            ["yes"] = "是",
            ["no"] = "否",
            ["error"] = "错误",
            ["warning"] = "警告",
            ["info"] = "信息",

            // Home
            ["home.nades"] = "道具",
            ["home.tricks"] = "技巧",

            // Trick edit
            ["trick_edit.title"] = "编辑点位",

            // Nearby target
            ["nearby.title"] = "发现附近落点",
            ["nearby.msg"] = "附近已存在落点 ''{0}''，是否直接为其添加站点？",
        }
    };

    public static void SetLanguage(string lang)
    {
        _lang = Strings.ContainsKey(lang) ? lang : "en";
    }

    public static string Get(string key)
    {
        if (Strings.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        return key;
    }

    public static string F(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
