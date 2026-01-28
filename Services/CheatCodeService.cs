using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameCheatHelper.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 秘籍代码服务
    /// 负责加载、管理和查询秘籍代码
    /// </summary>
    public class CheatCodeService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private List<CheatCode> _cheatCodes;
        private readonly string _defaultCheatsPath;

        /// <summary>
        /// 所有秘籍代码
        /// </summary>
        public IReadOnlyList<CheatCode> CheatCodes => _cheatCodes.AsReadOnly();

        /// <summary>
        /// 构造函数
        /// </summary>
        public CheatCodeService()
        {
            _cheatCodes = new List<CheatCode>();
            _defaultCheatsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DefaultCheats.json");
            Logger.Info("秘籍服务初始化");
        }

        /// <summary>
        /// 加载默认秘籍库
        /// </summary>
        public bool LoadDefaultCheats()
        {
            try
            {
                if (!File.Exists(_defaultCheatsPath))
                {
                    Logger.Error($"默认秘籍文件不存在: {_defaultCheatsPath}");
                    return false;
                }

                var json = File.ReadAllText(_defaultCheatsPath);
                var data = JObject.Parse(json);

                _cheatCodes.Clear();

                // 加载魔兽争霸3秘籍
                if (data["cheats"]?["warcraft3"] is JArray wc3Cheats)
                {
                    foreach (var item in wc3Cheats)
                    {
                        var cheat = item.ToObject<CheatCode>();
                        if (cheat != null)
                        {
                            _cheatCodes.Add(cheat);
                        }
                    }
                }

                // 加载星际争霸1秘籍
                if (data["cheats"]?["starcraft"] is JArray scCheats)
                {
                    foreach (var item in scCheats)
                    {
                        var cheat = item.ToObject<CheatCode>();
                        if (cheat != null)
                        {
                            _cheatCodes.Add(cheat);
                        }
                    }
                }

                Logger.Info($"成功加载 {_cheatCodes.Count} 个秘籍");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载默认秘籍失败");
                return false;
            }
        }

        /// <summary>
        /// 根据游戏类型获取秘籍列表
        /// </summary>
        public List<CheatCode> GetCheatsByGame(GameType gameType)
        {
            return _cheatCodes.Where(c => c.Game == gameType && c.Enabled).ToList();
        }

        /// <summary>
        /// 根据ID获取秘籍
        /// </summary>
        public CheatCode? GetCheatById(string id)
        {
            return _cheatCodes.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 搜索秘籍
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>匹配的秘籍列表</returns>
        public List<CheatCode> SearchCheats(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return _cheatCodes.Where(c => c.Enabled).ToList();
            }

            keyword = keyword.ToLower();
            return _cheatCodes.Where(c =>
                c.Enabled &&
                (c.Code.ToLower().Contains(keyword) ||
                 c.Description.ToLower().Contains(keyword) ||
                 c.Category.ToLower().Contains(keyword))
            ).ToList();
        }

        /// <summary>
        /// 添加自定义秘籍
        /// </summary>
        public bool AddCheat(CheatCode cheat)
        {
            try
            {
                if (string.IsNullOrEmpty(cheat.Id))
                {
                    cheat.Id = Guid.NewGuid().ToString();
                }

                if (_cheatCodes.Any(c => c.Id == cheat.Id))
                {
                    Logger.Warn($"秘籍ID已存在: {cheat.Id}");
                    return false;
                }

                _cheatCodes.Add(cheat);
                Logger.Info($"添加秘籍: {cheat.Code}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "添加秘籍失败");
                return false;
            }
        }

        /// <summary>
        /// 删除秘籍
        /// </summary>
        public bool RemoveCheat(string id)
        {
            try
            {
                var cheat = _cheatCodes.FirstOrDefault(c => c.Id == id);
                if (cheat == null)
                {
                    Logger.Warn($"秘籍不存在: {id}");
                    return false;
                }

                _cheatCodes.Remove(cheat);
                Logger.Info($"删除秘籍: {cheat.Code}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除秘籍失败");
                return false;
            }
        }

        /// <summary>
        /// 更新秘籍
        /// </summary>
        public bool UpdateCheat(CheatCode cheat)
        {
            try
            {
                var existingCheat = _cheatCodes.FirstOrDefault(c => c.Id == cheat.Id);
                if (existingCheat == null)
                {
                    Logger.Warn($"秘籍不存在: {cheat.Id}");
                    return false;
                }

                var index = _cheatCodes.IndexOf(existingCheat);
                _cheatCodes[index] = cheat;
                Logger.Info($"更新秘籍: {cheat.Code}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新秘籍失败");
                return false;
            }
        }

        /// <summary>
        /// 获取秘籍统计信息
        /// </summary>
        public Dictionary<GameType, int> GetCheatStatistics()
        {
            return _cheatCodes
                .Where(c => c.Enabled)
                .GroupBy(c => c.Game)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
