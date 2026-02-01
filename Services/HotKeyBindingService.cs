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
        private readonly string _dataDirectory;
        private Dictionary<GameType, List<HotKeyBinding>> _hotKeyBindingsByGame;

        /// <summary>
        /// 所有热键绑定（已废弃，使用 GetBindingsByGameType）
        /// </summary>
        [Obsolete("使用 GetBindingsByGameType 代替")]
        public IReadOnlyList<HotKeyBinding> HotKeyBindings =>
            _hotKeyBindingsByGame.Values.SelectMany(x => x).ToList().AsReadOnly();

        public HotKeyBindingService()
        {
            _hotKeyBindingsByGame = new Dictionary<GameType, List<HotKeyBinding>>();
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Logger.Info("热键绑定服务初始化");
        }

        /// <summary>
        /// 获取游戏类型对应的热键文件路径
        /// </summary>
        private string GetHotKeyFilePath(GameType gameType)
        {
            var fileName = gameType switch
            {
                GameType.Warcraft3 => "Warcraft3_HotKeys.json",
                GameType.StarCraft => "StarCraft_HotKeys.json",
                _ => "DefaultHotKeys.json"
            };
            return Path.Combine(_dataDirectory, fileName);
        }

        /// <summary>
        /// 加载指定游戏类型的热键绑定
        /// </summary>
        public bool LoadDefaultHotKeyBindings(GameType gameType)
        {
            try
            {
                var filePath = GetHotKeyFilePath(gameType);

                if (!File.Exists(filePath))
                {
                    Logger.Warn($"游戏 {gameType} 的热键文件不存在: {filePath}");
                    // 创建默认绑定
                    CreateDefaultBindings(gameType);
                    return true;
                }

                var json = File.ReadAllText(filePath);
                var bindings = JsonConvert.DeserializeObject<List<HotKeyBindingDto>>(json);

                if (bindings == null)
                {
                    Logger.Error($"解析游戏 {gameType} 的热键绑定失败");
                    return false;
                }

                // 初始化该游戏的绑定列表
                if (!_hotKeyBindingsByGame.ContainsKey(gameType))
                {
                    _hotKeyBindingsByGame[gameType] = new List<HotKeyBinding>();
                }

                _hotKeyBindingsByGame[gameType].Clear();
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
                    _hotKeyBindingsByGame[gameType].Add(binding);
                }

                Logger.Info($"成功加载游戏 {gameType} 的 {_hotKeyBindingsByGame[gameType].Count} 个热键绑定");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"加载游戏 {gameType} 的热键绑定失败");
                return false;
            }
        }

        /// <summary>
        /// 根据游戏类型获取热键绑定
        /// </summary>
        public List<HotKeyBinding> GetBindingsByGameType(GameType gameType, CheatCodeService cheatCodeService)
        {
            // 如果该游戏的热键还未加载，先加载
            if (!_hotKeyBindingsByGame.ContainsKey(gameType))
            {
                LoadDefaultHotKeyBindings(gameType);
            }

            // 获取该游戏的所有秘籍ID
            var cheats = cheatCodeService.GetCheatsByGame(gameType);
            var cheatIds = new HashSet<string>(cheats.Select(c => c.Id));

            // 返回该游戏的热键绑定（仅包含有效的秘籍ID）
            return _hotKeyBindingsByGame.ContainsKey(gameType)
                ? _hotKeyBindingsByGame[gameType].Where(b => cheatIds.Contains(b.CheatCodeId)).ToList()
                : new List<HotKeyBinding>();
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
        private void CreateDefaultBindings(GameType gameType)
        {
            var bindings = new List<HotKeyBinding>();

            if (gameType == GameType.Warcraft3)
            {
                bindings = new List<HotKeyBinding>
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
            else if (gameType == GameType.StarCraft)
            {
                bindings = new List<HotKeyBinding>
                {
                    new HotKeyBinding
                    {
                        Id = 1,
                        CheatCodeId = "sc_showmethemoney",
                        HotKey = new HotKey { Id = 1, Key = Key.F1, Modifiers = 0, CheatCodeId = "sc_showmethemoney" },
                        Description = "星际1: show me the money"
                    }
                };
            }

            _hotKeyBindingsByGame[gameType] = bindings;
            SaveHotKeyBindings(gameType);
        }

        /// <summary>
        /// 保存热键绑定到文件
        /// </summary>
        public bool SaveHotKeyBindings(GameType gameType)
        {
            try
            {
                if (!_hotKeyBindingsByGame.ContainsKey(gameType))
                {
                    Logger.Warn($"游戏 {gameType} 没有热键绑定数据");
                    return false;
                }

                var bindings = _hotKeyBindingsByGame[gameType];
                var dtoList = bindings.Select(b => new HotKeyBindingDto
                {
                    Id = b.Id,
                    CheatCodeId = b.CheatCodeId,
                    Key = b.HotKey.Key.ToString(),
                    Modifiers = b.HotKey.Modifiers,
                    Description = b.Description
                }).ToList();

                var json = JsonConvert.SerializeObject(dtoList, Formatting.Indented);
                var filePath = GetHotKeyFilePath(gameType);

                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
                Logger.Info($"游戏 {gameType} 的热键绑定已保存到: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"保存游戏 {gameType} 的热键绑定失败");
                return false;
            }
        }

        /// <summary>
        /// 添加或更新热键绑定
        /// </summary>
        /// <param name="gameType">游戏类型</param>
        /// <param name="cheatCodeId">秘籍ID</param>
        /// <param name="hotKey">热键（null表示移除热键）</param>
        /// <param name="description">描述</param>
        public bool AddOrUpdateHotKeyBinding(GameType gameType, string cheatCodeId, HotKey? hotKey, string description)
        {
            try
            {
                // 确保该游戏的热键列表已初始化
                if (!_hotKeyBindingsByGame.ContainsKey(gameType))
                {
                    LoadDefaultHotKeyBindings(gameType);
                }

                var bindings = _hotKeyBindingsByGame[gameType];

                // 查找现有绑定
                var existingBinding = bindings.FirstOrDefault(b => b.CheatCodeId == cheatCodeId);

                if (hotKey == null)
                {
                    // 移除热键绑定
                    if (existingBinding != null)
                    {
                        bindings.Remove(existingBinding);
                        Logger.Info($"移除游戏 {gameType} 秘籍 {cheatCodeId} 的热键绑定");
                    }
                }
                else
                {
                    if (existingBinding != null)
                    {
                        // 更新现有绑定
                        existingBinding.HotKey = hotKey;
                        existingBinding.Description = description;
                        Logger.Info($"更新游戏 {gameType} 秘籍 {cheatCodeId} 的热键为: {hotKey.DisplayText}");
                    }
                    else
                    {
                        // 添加新绑定
                        var newId = bindings.Any() ? bindings.Max(b => b.Id) + 1 : 1;
                        var binding = new HotKeyBinding
                        {
                            Id = newId,
                            CheatCodeId = cheatCodeId,
                            HotKey = hotKey,
                            Description = description
                        };
                        bindings.Add(binding);
                        Logger.Info($"添加游戏 {gameType} 秘籍 {cheatCodeId} 的热键绑定: {hotKey.DisplayText}");
                    }
                }

                // 保存到文件
                return SaveHotKeyBindings(gameType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"添加或更新游戏 {gameType} 的热键绑定失败");
                return false;
            }
        }

        /// <summary>
        /// 根据秘籍ID获取热键绑定
        /// </summary>
        public HotKeyBinding? GetBindingByCheatCodeId(GameType gameType, string cheatCodeId)
        {
            if (!_hotKeyBindingsByGame.ContainsKey(gameType))
            {
                LoadDefaultHotKeyBindings(gameType);
            }

            return _hotKeyBindingsByGame[gameType].FirstOrDefault(b => b.CheatCodeId == cheatCodeId);
        }

        /// <summary>
        /// 检查热键是否被占用（仅在同一游戏内检查）
        /// </summary>
        /// <param name="gameType">游戏类型</param>
        /// <param name="hotKey">要检查的热键</param>
        /// <param name="excludeCheatCodeId">排除的秘籍ID（编辑时使用）</param>
        /// <returns>占用该热键的秘籍ID，如果未被占用返回null</returns>
        public string? CheckHotKeyOccupied(GameType gameType, HotKey hotKey, string? excludeCheatCodeId = null)
        {
            if (!_hotKeyBindingsByGame.ContainsKey(gameType))
            {
                LoadDefaultHotKeyBindings(gameType);
            }

            foreach (var binding in _hotKeyBindingsByGame[gameType])
            {
                if (excludeCheatCodeId != null && binding.CheatCodeId == excludeCheatCodeId)
                {
                    continue;
                }

                if (binding.HotKey.IsSameAs(hotKey))
                {
                    return binding.CheatCodeId;
                }
            }
            return null;
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
