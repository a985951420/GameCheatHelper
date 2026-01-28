using System;
using System.Threading.Tasks;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    /// <summary>
    /// 秘籍执行器
    /// 负责在游戏中自动输入秘籍代码
    /// </summary>
    public class CheatExecutor
    {
        private readonly KeyboardSimulator _keyboard;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputDelay">键盘输入延迟（毫秒）</param>
        public CheatExecutor(int inputDelay = 10)
        {
            _keyboard = new KeyboardSimulator(inputDelay);
        }

        /// <summary>
        /// 执行秘籍输入
        /// </summary>
        /// <param name="cheat">要执行的秘籍</param>
        /// <param name="gameWindowHandle">游戏窗口句柄</param>
        /// <returns>执行是否成功</returns>
        public async Task<bool> ExecuteCheatAsync(CheatCode cheat, IntPtr gameWindowHandle)
        {
            try
            {
                // 1. 验证参数
                if (cheat == null || string.IsNullOrEmpty(cheat.Code))
                {
                    return false;
                }

                if (gameWindowHandle == IntPtr.Zero)
                {
                    return false;
                }

                // 2. 激活游戏窗口
                if (!Win32API.SetForegroundWindow(gameWindowHandle))
                {
                    return false;
                }

                // 等待窗口激活
                await Task.Delay(100);

                // 3. 打开聊天框（按下 Enter 键）
                _keyboard.PressEnter();
                await Task.Delay(50);

                // 4. 输入秘籍代码
                _keyboard.TypeText(cheat.Code);
                await Task.Delay(50);

                // 5. 确认输入（再次按 Enter）
                _keyboard.PressEnter();

                return true;
            }
            catch (Exception)
            {
                // 执行失败，记录日志
                return false;
            }
        }

        /// <summary>
        /// 同步执行秘籍
        /// </summary>
        public bool ExecuteCheat(CheatCode cheat, IntPtr gameWindowHandle)
        {
            return ExecuteCheatAsync(cheat, gameWindowHandle).GetAwaiter().GetResult();
        }
    }
}
