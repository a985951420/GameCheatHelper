using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GameCheatHelper.Core.Models;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    /// <summary>
    /// 秘籍执行器
    /// 负责在游戏中自动输入秘籍代码，支持全屏模式和多种输入方式
    /// </summary>
    public class CheatExecutor
    {
        private readonly KeyboardSimulator _keyboard;
        private readonly int _maxRetries;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputDelay">键盘输入延迟（毫秒），默认10ms</param>
        /// <param name="maxRetries">最大重试次数，默认2次</param>
        public CheatExecutor(int inputDelay = 10, int maxRetries = 2)
        {
            _keyboard = new KeyboardSimulator(inputDelay, InputMethod.Auto);
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// 执行秘籍输入 - 增强版，支持全屏模式和自动重试
        /// </summary>
        /// <param name="cheat">要执行的秘籍</param>
        /// <param name="gameWindowHandle">游戏窗口句柄</param>
        /// <returns>执行是否成功</returns>
        public async Task<bool> ExecuteCheatAsync(CheatCode cheat, IntPtr gameWindowHandle)
        {
            Debug.WriteLine($"========== 开始执行秘籍: {cheat?.Code} ({cheat?.Description}) ==========");

            try
            {
                // 1. 验证参数
                if (cheat == null || string.IsNullOrEmpty(cheat.Code))
                {
                    Debug.WriteLine("❌ 秘籍参数无效");
                    return false;
                }

                if (!WindowHelper.IsWindowValid(gameWindowHandle))
                {
                    Debug.WriteLine("❌ 游戏窗口句柄无效");
                    return false;
                }

                Debug.WriteLine($"秘籍代码: {cheat.Code}");
                Debug.WriteLine($"窗口状态: {WindowHelper.GetWindowStateDescription(gameWindowHandle)}");

                // 2. 检测窗口状态
                bool isFullScreen = WindowHelper.IsFullScreen(gameWindowHandle);
                Debug.WriteLine($"全屏模式: {isFullScreen}");

                // 3. 激活游戏窗口（使用增强方法，支持全屏）
                Debug.WriteLine("正在激活游戏窗口...");
                bool windowActivated = await WindowHelper.ActivateWindowAsync(gameWindowHandle, maxRetries: 3);

                if (!windowActivated)
                {
                    Debug.WriteLine("❌ 窗口激活失败");
                    return false;
                }

                Debug.WriteLine("✅ 窗口激活成功");

                // 4. 根据窗口状态调整延迟（全屏模式需要更长时间）
                int chatOpenDelay = isFullScreen ? 100 : 50;
                int inputDelay = isFullScreen ? 100 : 50;

                // 5. 执行输入（带重试机制）
                for (int attempt = 1; attempt <= _maxRetries; attempt++)
                {
                    Debug.WriteLine($"\n--- 输入尝试 {attempt}/{_maxRetries} ---");

                    bool useAlternativeInput = attempt > 1; // 第二次尝试使用备选输入方式

                    if (useAlternativeInput)
                    {
                        Debug.WriteLine("⚡ 使用备选输入方式 (keybd_event)");
                    }

                    try
                    {
                        // 5.1 打开聊天框（按下 Enter 键）
                        Debug.WriteLine("按下 Enter 打开聊天框...");
                        _keyboard.PressEnter(useAlternativeInput);
                        await Task.Delay(chatOpenDelay);

                        // 5.2 输入秘籍代码
                        Debug.WriteLine($"输入秘籍代码: {cheat.Code}");
                        _keyboard.TypeText(cheat.Code, useAlternativeInput);
                        await Task.Delay(inputDelay);

                        // 5.3 确认输入（再次按 Enter）
                        Debug.WriteLine("按下 Enter 确认输入...");
                        _keyboard.PressEnter(useAlternativeInput);
                        await Task.Delay(50);

                        Debug.WriteLine($"✅ 秘籍执行成功 (尝试 {attempt}/{_maxRetries})");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ 输入执行异常 (尝试 {attempt}/{_maxRetries}): {ex.Message}");

                        if (attempt < _maxRetries)
                        {
                            Debug.WriteLine("等待后重试...");
                            await Task.Delay(200);
                        }
                    }
                }

                Debug.WriteLine($"❌ 秘籍执行失败，已用尽所有 {_maxRetries} 次尝试");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 秘籍执行严重错误: {ex.Message}");
                Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
            finally
            {
                Debug.WriteLine($"========== 秘籍执行结束 ==========\n");
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
