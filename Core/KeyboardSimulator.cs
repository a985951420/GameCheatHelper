using System;
using System.Runtime.InteropServices;
using System.Threading;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    /// <summary>
    /// 键盘输入模拟器
    /// 通过 Windows SendInput API 模拟键盘输入
    /// </summary>
    public class KeyboardSimulator
    {
        private readonly int _delayBetweenKeys;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="delayBetweenKeys">按键之间的延迟（毫秒），默认10ms</param>
        public KeyboardSimulator(int delayBetweenKeys = 10)
        {
            _delayBetweenKeys = delayBetweenKeys;
        }

        /// <summary>
        /// 输入文本字符串
        /// </summary>
        /// <param name="text">要输入的文本</param>
        public void TypeText(string text)
        {
            foreach (char c in text)
            {
                TypeCharacter(c);
                if (_delayBetweenKeys > 0)
                {
                    Thread.Sleep(_delayBetweenKeys);
                }
            }
        }

        /// <summary>
        /// 输入单个字符（使用 Unicode 方式）
        /// </summary>
        /// <param name="character">要输入的字符</param>
        private void TypeCharacter(char character)
        {
            var inputs = new Win32API.INPUT[2];

            // 按下键
            inputs[0].Type = Win32API.INPUT_KEYBOARD;
            inputs[0].Union.KeyboardInput.Vk = 0;
            inputs[0].Union.KeyboardInput.Scan = character;
            inputs[0].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_UNICODE;
            inputs[0].Union.KeyboardInput.Time = 0;
            inputs[0].Union.KeyboardInput.ExtraInfo = IntPtr.Zero;

            // 释放键
            inputs[1].Type = Win32API.INPUT_KEYBOARD;
            inputs[1].Union.KeyboardInput.Vk = 0;
            inputs[1].Union.KeyboardInput.Scan = character;
            inputs[1].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_UNICODE | Win32API.KEYEVENTF_KEYUP;
            inputs[1].Union.KeyboardInput.Time = 0;
            inputs[1].Union.KeyboardInput.ExtraInfo = IntPtr.Zero;

            // 发送输入
            Win32API.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32API.INPUT)));
        }

        /// <summary>
        /// 按下特殊键（如 Enter, Escape 等）
        /// </summary>
        /// <param name="virtualKeyCode">虚拟键码</param>
        public void PressKey(ushort virtualKeyCode)
        {
            var inputs = new Win32API.INPUT[2];

            // 按下键
            inputs[0].Type = Win32API.INPUT_KEYBOARD;
            inputs[0].Union.KeyboardInput.Vk = virtualKeyCode;
            inputs[0].Union.KeyboardInput.Scan = 0;
            inputs[0].Union.KeyboardInput.Flags = 0;
            inputs[0].Union.KeyboardInput.Time = 0;
            inputs[0].Union.KeyboardInput.ExtraInfo = IntPtr.Zero;

            // 释放键
            inputs[1].Type = Win32API.INPUT_KEYBOARD;
            inputs[1].Union.KeyboardInput.Vk = virtualKeyCode;
            inputs[1].Union.KeyboardInput.Scan = 0;
            inputs[1].Union.KeyboardInput.Flags = Win32API.KEYEVENTF_KEYUP;
            inputs[1].Union.KeyboardInput.Time = 0;
            inputs[1].Union.KeyboardInput.ExtraInfo = IntPtr.Zero;

            // 发送输入
            Win32API.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32API.INPUT)));
        }

        /// <summary>
        /// 按下 Enter 键
        /// </summary>
        public void PressEnter()
        {
            PressKey(Win32API.VK_RETURN);
        }

        /// <summary>
        /// 按下 Escape 键
        /// </summary>
        public void PressEscape()
        {
            PressKey(Win32API.VK_ESCAPE);
        }

        /// <summary>
        /// 按下 Backspace 键
        /// </summary>
        public void PressBackspace()
        {
            PressKey(Win32API.VK_BACK);
        }
    }
}
