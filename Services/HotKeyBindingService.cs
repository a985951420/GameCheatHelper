using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GameCheatHelper.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 热键绑定服务
    /// 负责管理热键与秘籍的绑定关系
    /// </summary>
    public class HotKeyBindingService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _defaultHotKeysPath;
        private List<HotKeyBinding> _hotKeyBindings;

        /// <summary>
        /// 所有热键绑定
        /// </summary>
        public IReadOnlyList<HotKeyBinding> HotKeyBindings => _hotKeyBindings.AsReadOnly();

        public HotKeyBindingService()
        {
            _hotKeyBindings = new List<HotKeyBinding>();
            _defaultHotKeysPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DefaultHotKeys.json");
            Logger.Info("热键绑定服务初始化");
        }

        /// <summary>
        /// 加载默认热键绑定
        /// </summary>
        public bool LoadDefaultHotKeyBindings()
        {
            try
            {
                if (!File.Exists(_defaultHotKeysPath))
                {
                    Logger.Warn($"默认热键文件不存在: {_defaultHotKeysPath}");
                    // 创建默认绑定
                    CreateDefaultBindings();
                    return true;
                }

                var json = File.ReadAllText(_defaultHotKeysPath);
                var bindings = JsonConvert.DeserializeObject<List<HotKeyBindingDto>>(json);

                if (bindings == null)
                {
                    Logger.Error("解析热键绑定失败");
                    return false;
                }

                _hotKeyBindings.Clear();
                foreach (var dto in bindings)
                {
                    var binding = new HotKeyBinding
                    {
                        Id = dto.Id,
                        CheatCodeId = dto.CheatCodeId,
                        HotKey = new HotKey
                        {
                            Id = dto.Id,
                            Key = ParseKey(dto.Key),
                            Modifiers = dto.Modifiers,
                            CheatCodeId = dto.CheatCodeId
                        },
                        Description = dto.Description
                    };
                    _hotKeyBindings.Add(binding);
                }

                Logger.Info($"成功加载 {_hotKeyBindings.Count} 个热键绑定");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载默认热键绑定失败");
                return false;
            }
        }

        /// <summary>
        /// 根据游戏类型获取热键绑定
        /// </summary>
        public List<HotKeyBinding> GetBindingsByGameType(GameType gameType, CheatCodeService cheatCodeService)
        {
            var cheats = cheatCodeService.GetCheatsByGame(gameType);
            var cheatIds = new HashSet<string>(cheats.Select(c => c.Id));

            return _hotKeyBindings.Where(b => cheatIds.Contains(b.CheatCodeId)).ToList();
        }

        /// <summary>
        /// 解析按键字符串
        /// </summary>
        private Key ParseKey(string keyString)
        {
            if (Enum.TryParse<Key>(keyString, true, out var key))
            {
                return key;
            }
            Logger.Warn($"无法解析按键: {keyString}, 使用默认值 F1");
            return Key.F1;
        }

        /// <summary>
        /// 创建默认绑定
        /// </summary>
        private void CreateDefaultBindings()
        {
            _hotKeyBindings = new List<HotKeyBinding>
            {
                new HotKeyBinding
                {
                    Id = 1,
                    CheatCodeId = "wc3_greedisgood",
                    HotKey = new HotKey { Id = 1, Key = Key.F1, Modifiers = 0, CheatCodeId = "wc3_greedisgood" },
                    Description = "魔兽3: greedisgood"
                },
                new HotKeyBinding
                {
                    Id = 2,
                    CheatCodeId = "wc3_iseedeadpeople",
                    HotKey = new HotKey { Id = 2, Key = Key.F2, Modifiers = 0, CheatCodeId = "wc3_iseedeadpeople" },
                    Description = "魔兽3: iseedeadpeople"
                },
                new HotKeyBinding
                {
                    Id = 3,
                    CheatCodeId = "wc3_whosyourdaddy",
                    HotKey = new HotKey { Id = 3, Key = Key.F3, Modifiers = 0, CheatCodeId = "wc3_whosyourdaddy" },
                    Description = "魔兽3: whosyourdaddy"
                }
            };
        }

        /// <summary>
        /// DTO 类用于 JSON 反序列化
        /// </summary>
        private class HotKeyBindingDto
        {
            public int Id { get; set; }
            public string CheatCodeId { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
            public uint Modifiers { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// 热键绑定数据
    /// </summary>
    public class HotKeyBinding
    {
        public int Id { get; set; }
        public string CheatCodeId { get; set; } = string.Empty;
        public HotKey HotKey { get; set; } = new HotKey();
        public string Description { get; set; } = string.Empty;
    }
}
