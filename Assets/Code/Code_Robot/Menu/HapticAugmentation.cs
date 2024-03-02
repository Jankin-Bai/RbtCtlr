using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TouchRobot.Extra
{
    // 非静态,每个游戏对象对应一个
    public class AugmentedHapticModule
    {
        // 模块化编程可以减少之后的痛苦
        // 这个模块专司实现增强触觉.对反馈力进行滤波也可视作"增强"的一部分.
        // <增强了什么>
        // 为什么不将该部分与单位换算糅在一起?->设想一下万一之后想要改代码的时候会不会想杀了上一个程序员?这是研究性质的并不是一锤定音的代码

        // 野生的构造器
        internal AugmentedHapticModule()
        {
            list_wave_former = new List<HapticWaveformer>();
            list_wave_former_to_remove = new List<HapticWaveformer>();
            InvokeModulation();
        }
        // collision_exit就是自由状态,没跟任何物品碰撞的咯
        internal enum haptic_event { collision_exit = 0, collision_enter = 1, collision_stay = 2, untitled_event_00 = 3, untitled_event_01 = 4 }
        // 正在运行中的波形/待结束的波形
        List<HapticWaveformer> list_wave_former;
        List<HapticWaveformer> list_wave_former_to_remove;
        // 根据名称自建波形并继承自该类
        public enum wave_name { }
        internal delegate float Modulate(float input);
        internal Modulate modulate;
        // 每次更改/初始化时调用,确定是否需要波形调制,并赋值委托
        internal void InvokeModulation()
        {
            if (!host_enable_augmented_haptic)
            {
                modulate = Empty;
                return;
            }
            if (!augmented_haptic)
            {
                modulate = Empty;
                return;
            }
            modulate = Calculate;
        }
        private float Empty(float val = 0) { return 0; }
        private float Calculate(float val)
        {
            float sum = 0;
            foreach (HapticWaveformer hf in list_wave_former)
            {
                sum += hf.move_buffer(val);
            }
            ModifyCurrentList();
            return 0;
        }
        // 外部调制是否开启增强的触觉反馈
        public static void EnableAugmentedHaptic(bool to = true)
        {
            augmented_haptic = to;
        }
        internal void HostEnableAugmentedHaptic(bool to = true)
        {
            host_enable_augmented_haptic = to;
        }
        // 启用则增加触觉事件调制的力反馈.静态变量为全局管理,非静态仅针对对应gameController
        private static bool augmented_haptic = false;
        private bool host_enable_augmented_haptic = false;
        // 子波生命周期结束时将被加入移除名单
        internal void AddToList(HapticWaveformer hw)
        {
            list_wave_former_to_remove.Add(hw);
        }
        private void ModifyCurrentList()
        {
            foreach (HapticWaveformer hw in list_wave_former_to_remove)
            {
                list_wave_former.Remove(hw);
            }
            if (list_wave_former_to_remove.Count > 0)
                list_wave_former_to_remove = new List<HapticWaveformer>();
        }
        // 
    }
    class HapticWaveformer
    {
        public void AssignGeneralParameter(AugmentedHapticModule module, int fms = 50, bool hk = false, int hk_dl = 0)
        {
            my_module = module;
            wave_frames = fms;
            use_hook = hk;
            hook_delay_frame = hk_dl;
            first_in = true;
            hook_delay_buffer = new List<float>();
            if (use_hook)
            {
                move_buffer = MoveOn;
            }
            else
            {
                move_buffer = Empty;
            }
        }
        protected AugmentedHapticModule my_module;
        // 波形播放的帧序.访问帧序的情景:1. 何时结束; 2. 延迟; 3. f(t)是分段函数
        protected int current_frame = 0;
        // 在多少帧后删除该波形
        private int wave_frames = 50;
        // 如果使用hook,则生成波形依赖实时(或延迟)的基量
        private bool use_hook = false;
        private bool first_in = true;
        private float locked_value = 0;
        // 如果使用了hook,其延迟为多少?
        private int hook_delay_frame = 0;
        // 一个FIFO的载波幅度延时缓冲器(静态数组写起来太麻烦了)
        private List<float> hook_delay_buffer;
        internal delegate float MoveBuffer(float val);
        internal MoveBuffer move_buffer;
        // 如果输入的是f(0).base函数输出用于调制的基
        internal virtual float Empty(float val)
        {
            if (current_frame++ > wave_frames)
            {
                my_module.AddToList(this);
                return 0;
            }
            if (first_in)
            {
                locked_value = val;
                first_in = false;
                return Calculate(locked_value);
            }
            else
            {
                return Calculate(locked_value);
            }
        }
        // 如果输入的是f(t)或f(t+delta·t).base函数输出用于调制的基,并在重载中实现调制
        internal virtual float MoveOn(float val)
        {
            if (current_frame++ > wave_frames)
            {
                my_module.AddToList(this);
                return 0;
            }
            hook_delay_buffer.Add(val);
            if (current_frame > hook_delay_frame)
            {
                float value = hook_delay_buffer[0];
                hook_delay_buffer.RemoveAt(0);
                return Calculate(value);
            }
            return 0;
        }
        internal virtual float Calculate(float val)
        {
            return 0;
        }
    }
    class HapticWaveformer_DualPeakExponential : HapticWaveformer
    {
        float parameter_a, parameter_b, parameter_c;
        public HapticWaveformer_DualPeakExponential() { }
        public HapticWaveformer_DualPeakExponential(AugmentedHapticModule module, int pa, int pb, int pc, int fms = 50, bool hk = false, int hk_dl = 0)
        {
            AssignGeneralParameter(module, fms, hk, hk_dl);
        }
        internal override float Calculate(float val)
        {
            // to be filled
            return val;
        }
    }
}