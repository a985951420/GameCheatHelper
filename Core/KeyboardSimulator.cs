using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using GameCheatHelper.Utilities;

namespace GameCheatHelper.Core
{
    /// <summary>
    /// 键盘输入方式
    /// </summary>
    public enum InputMethod
    {
        /// <summary>
        /// SendInput - 现代推荐方式，兼容性好
        /// </summary>
        SendInput,

        /// <summary>
        /// keybd_event - 传统方式，某些全屏游戏中更有效
        /// </summary>
        KeybdEvent,

        /// <summary>
        /// 自动选择 - 先尝试 SendInput，失败则使用 keybd_event
        /// </summary>
        Auto
    }

    /// <summary>
    /// 键盘输入模拟器
    /// 通过 Windows SendInput API 或 keybd_event 模拟键盘输入
    /// </summary>
    public class KeyboardSimulator
    {
        private readonly int _delayBetweenKeys;
        private InputMethod _preferredMethod;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="delayBetweenKeys">按键之间的延迟（毫秒），默认10ms</param>
        /// <param name="preferredMethod">首选输入方式，默认自动选择</param>
        public KeyboardSimulator(int delayBetweenKeys = 10, InputMethod preferredMethod = InputMethod.Auto)
        {
            _delayBetweenKeys = delayBetweenKeys;
            _preferredMethod = preferredMethod;
        }

        /// <summary>
        /// 输入文本字符串
        /// </summary>
        /// <param name="text">要输入的文本</param>
        /// <param name="useAlternative">是否强制使用备选方法</param>
        public void TypeText(string text, bool useAlternative = false)
        {
            var method = useAlternative ? InputMethod.KeybdEvent : _preferredMethod;
            Debug.WriteLine($"输入文本使用方法: {method}");

            foreach (char c in text)
            {
                TypeCharacter(c, method);
                if (_delayBetweenKeys > 0)
                {
                    Thread.Sleep(_delayBetweenKeys);
                }
            }
        }

        /// <summary>
        /// 输入单个字符（支持多种输入方式）
        /// </summary>
        /// <param name="character">要输入的字符</param>
        /// <param name="method">输入方法</param>
        private void TypeCharacter(char character, InputMethod method = InputMethod.SendInput)
        {
            switch (method)
            {
                case InputMethod.SendInput:
                    TypeCharacterWithSendInput(character);
                    break;

                case InputMethod.KeybdEvent:
                    TypeCharacterWithKeybdEvent(character);
                    break;

                case InputMethod.Auto:
                    // 先尝试 SendInput，失败则使用 keybd_event
                    try
                    {
                        TypeCharacterWithSendInput(character);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SendInput 失败，切换到 keybd_event: {ex.Message}");
                        TypeCharacterWithKeybdEvent(character);
                    }
                    break;
            }
        }

        /// <summary>
        /// 使用 SendInput 输入单个字符（Unicode 方式）
        /// </summary>
        private void TypeCharacterWithSendInput(char character)
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
            uint result = Win32API.SendInput(2, inputs, Marshal.SizeOf(typeof(Win32API.INPUT)));
            if (result != 2)
            {
                Debug.WriteLine($"SendInput 返回值异常: {result}, 预期: 2");
            }
        }

        /// <summary>
        /// 使用 keybd_event 输入单个字符（传统方式，对某些全屏游戏更有效）
        /// </summary>
        private void TypeCharacterWithKeybdEvent(char character)
        {
            // keybd_event 不直接支持 Unicode，需要使用虚拟键码
            // 对于 ASCII 字符，可以使用 VkKeyScan 转换
            short vkAndShift = VkKeyScan(character);

            if (vkAndShift == -1)
            {
                // 不支持的字符，回退到 SendInput
                Debug.WriteLine($"字符 '{character}' 无法使用 keybd_event，回退到 SendInput");
                TypeCharacterWithSendInput(character);
                return;
            }

            byte vk = (byte)(vkAndShift & 0xFF);
            byte shiftState = (byte)((vkAndShift >> 8) & 0xFF);

            // 处理 Shift 键
            bool needShift = (shiftState & 0x01) != 0;

            if (needShift)
            {
                Win32API.keybd_event((byte)Win32API.VK_SHIFT, 0, 0, UIntPtr.Zero);
                Thread.Sleep(5);
            }

            // 按下字符键
            Win32API.keybd_event(vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(5);
            Win32API.keybd_event(vk, 0, Win32API.KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (needShift)
            {
                Thread.Sleep(5);
                Win32API.keybd_event((byte)Win32API.VK_SHIFT, 0, Win32API.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        /// <summary>
        /// 将字符转换为虚拟键码
        /// </summary>
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        /// <summary>
        /// 按下特殊键（如 Enter, Escape 等）
        /// </summary>
        /// <param name="virtualKeyCode">虚拟键码</param>
        /// <param name="useAlternative">是否使用备选方法</param>
        public void PressKey(ushort virtualKeyCode, bool useAlternative = false)
        {
            var method = useAlternative ? InputMethod.KeybdEvent : _preferredMethod;
            Debug.WriteLine($"按下特殊键 {virtualKeyCode} 使用方法: {method}");

            if (method == InputMethod.KeybdEvent || (method == InputMethod.Auto && useAlternative))
            {
                PressKeyWithKeybdEvent(virtualKeyCode);
            }
            else
            {
                PressKeyWithSendInput(virtualKeyCode);
            }
        }

        /// <summary>
        /// 使用 SendInput 按下特殊键
        /// </summary>
        private void PressKeyWithSendInput(ushort virtualKeyCode)
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
        /// 使用 keybd_event 按下特殊键
        /// </summary>
        private void PressKeyWithKeybdEvent(ushort virtualKeyCode)
        {
            Win32API.keybd_event((byte)virtualKeyCode, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            Win32API.keybd_event((byte)virtualKeyCode, 0, Win32API.KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        /// <summary>
        /// 按下 Enter 键
        /// </summary>
        /// <param name="useAlternative">是否使用备选输入方式</param>
        public void PressEnter(bool useAlternative = false)
        {
            PressKey(Win32API.VK_RETURN, useAlternative);
        }

        /// <summary>
        /// 按下 Escape 键
        /// </summary>
        /// <param name="useAlternative">是否使用备选输入方式</param>
        public void PressEscape(bool useAlternative = false)
        {
            PressKey(Win32API.VK_ESCAPE, useAlternative);
        }

        /// <summary>
        /// 按下 Backspace 键
        /// </summary>
        /// <param name="useAlternative">是否使用备选输入方式</param>
        public void PressBackspace(bool useAlternative = false)
        {
            PressKey(Win32API.VK_BACK, useAlternative);
        }
    }
}
