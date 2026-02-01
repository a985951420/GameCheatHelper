using System;
using System.Collections.Generic;
using System.Linq;
using GameCheatHelper.Core.Models;

namespace GameCheatHelper.ViewModels
{
    /// <summary>
    /// 秘籍编辑对话框 ViewModel
    /// </summary>
    public class CheatEditViewModel : ViewModelBase
    {
        private string _cheatCode = string.Empty;
        private string _description = string.Empty;
        private string _selectedCategory = "其他";
        private KeyValuePair<int, string> _selectedGameType;
        private bool _isEnabled = true;
        private string _validationMessage = string.Empty;
        private bool _isNewCheat;
        private HotKey? _currentHotKey;

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle => _isNewCheat ? "添加新秘籍" : "编辑秘籍";

        /// <summary>
        /// 游戏类型列表
        /// </summary>
        public List<KeyValuePair<int, string>> GameTypes { get; }

        /// <summary>
        /// 分类列表
        /// </summary>
        public List<string> Categories { get; }

        /// <summary>
        /// 当前热键
        /// </summary>
        public HotKey? CurrentHotKey
        {
            get => _currentHotKey;
            set => SetProperty(ref _currentHotKey, value);
        }

        /// <summary>
        /// 秘籍代码
        /// </summary>
        public string CheatCode
        {
            get => _cheatCode;
            set
            {
                SetProperty(ref _cheatCode, value);
                ValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                SetProperty(ref _description, value);
                ValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// 选中的分类
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        /// <summary>
        /// 选中的游戏类型
        /// </summary>
        public KeyValuePair<int, string> SelectedGameType
        {
            get => _selectedGameType;
            set => SetProperty(ref _selectedGameType, value);
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 编辑的秘籍（用于返回结果）
        /// </summary>
        public CheatCode? ResultCheat { get; private set; }

        /// <summary>
        /// 构造函数 - 新建秘籍
        /// </summary>
        public CheatEditViewModel()
        {
            _isNewCheat = true;

            GameTypes = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>((int)GameType.Warcraft3, "魔兽争霸3"),
                new KeyValuePair<int, string>((int)GameType.StarCraft, "星际争霸1")
            };

            Categories = new List<string>
            {
                "资源", "战斗", "地图", "建造", "科技", "游戏", "其他"
            };

            _selectedGameType = GameTypes.First();
        }

        /// <summary>
        /// 构造函数 - 编辑现有秘籍
        /// </summary>
        public CheatEditViewModel(CheatCode cheat) : this()
        {
            _isNewCheat = false;

            CheatCode = cheat.Code;
            Description = cheat.Description;
            SelectedCategory = cheat.Category;
            IsEnabled = cheat.Enabled;
            SelectedGameType = GameTypes.FirstOrDefault(g => g.Key == (int)cheat.Game);

            // 保存原始秘籍引用
            ResultCheat = cheat;

            // 加载热键（如果有的话）
            // 注意：这里暂时不加载，因为需要从 HotKeyBindingService 获取
            // 在调用代码中应该设置 CurrentHotKey
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(CheatCode))
            {
                ValidationMessage = "请输入秘籍代码";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ValidationMessage = "请输入秘籍描述";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedCategory))
            {
                SelectedCategory = "其他";
            }

            return true;
        }

        /// <summary>
        /// 获取秘籍对象
        /// </summary>
        public CheatCode GetCheatCode()
        {
            if (ResultCheat == null)
            {
                // 新建秘籍
                ResultCheat = new CheatCode
                {
                    Id = GenerateCheatId(),
                    Code = CheatCode.Trim(),
                    Description = Description.Trim(),
                    Category = SelectedCategory,
                    Game = (GameType)SelectedGameType.Key,
                    Enabled = IsEnabled
                };
            }
            else
            {
                // 更新现有秘籍
                ResultCheat.Code = CheatCode.Trim();
                ResultCheat.Description = Description.Trim();
                ResultCheat.Category = SelectedCategory;
                ResultCheat.Game = (GameType)SelectedGameType.Key;
                ResultCheat.Enabled = IsEnabled;
            }

            return ResultCheat;
        }

        /// <summary>
        /// 生成秘籍ID
        /// </summary>
        private string GenerateCheatId()
        {
            var gamePrefix = SelectedGameType.Key == (int)GameType.Warcraft3 ? "wc3" : "sc";
            var codeShort = CheatCode.Trim().Replace(" ", "").ToLower();
            if (codeShort.Length > 20)
            {
                codeShort = codeShort.Substring(0, 20);
            }
            return $"{gamePrefix}_{codeShort}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
