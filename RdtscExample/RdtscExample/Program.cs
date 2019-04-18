using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RdtscExample
{
    class Program
    {
        // Win-Api функция позволяющая вызывать (выделить область в памяти где мы можем что-то запускать)
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        // Возвращает указатель
        static IntPtr Alloc(byte[] asm)
        {
            // Получение указателя из области памяти, можешь запускть код
            var ptr = VirtualAlloc(IntPtr.Zero, (uint)asm.Length, 0x00001000, 0x40);
            Marshal.Copy(asm, 0, ptr, asm.Length);

            return ptr;
        }

        delegate long RdtscDelegate();

        // Нужен делегат, который бы указывал на этот сегмент
        private static byte[] rdtscAsm =
        {
            0x0F, 0x31, // rdtsc
            0xC3 // ret
        };

        static void Main(string[] args)
        {
            // Массив байт "превращаем" в делегат
            var rdtsc = Marshal.GetDelegateForFunctionPointer<RdtscDelegate>(Alloc(rdtscAsm));

            // Сравним машинную интсрукцию rdtsc и StopWatch: кто как замеряет время
            // Засекаем значение раз
            var a = rdtsc();
            var sp1 = Stopwatch.GetTimestamp();

            Thread.Sleep(1000);

            // Засекаем значение два
            var b = rdtsc();
            var sp2 = Stopwatch.GetTimestamp();

            // Посчитаем разницу между машиной и StopWatch: временной интервал секунда, сколько тиков ожидается
            var rdtscDiff = b - a;
            var spDiff = sp2 - sp1;
            Console.WriteLine($"Rdtsc: {rdtscDiff}");
            Console.WriteLine($"SW {spDiff}");
            Console.WriteLine($"Diff: {rdtscDiff / (double)spDiff}");

            // Есть машинная инструкция, которая работает примерно на чистоте процессора
            // которая позволяет супер точно замерять время, зачет StopWatch?
            // Почему не нравится? частота действительно может меняться, но Intel придумал
            // инвариантый TimeStampCounter: дает стабильно кол-во тик в сек и на одной частоте (на соврем процах все ок)
            // Так почему? Я поток, запустился и начал что то делать дернул 
            // иннструкцию rdtsc(ядро скажи мне значение счетчика) получили значение,
            // и вдруго внезапно Ос перекидывает поток на другое ядро: есть у процессов штука
            // ProcessorInfinity маска показыающая на каких ядрах может испольняться тек поток-процесс
            // По умолчанию никаких ограничений нет, если я гвозями не прибил поток к ядру меня может перекинуть на
            // Другое ядро, и у этого ядра может быть другой счетик, который не синхронизируем с первым. 
            // Rdtsc - хардвардный счетчик. Microsoft сделал это за нас и вывела Api(из kernel32.dll):  
            // QueryPerfomanceCounter, QueryPerfomanceFrequensy: общий интерфес для доутспа к таймеру выского ращрешения
            // Microsoft: не рекомендует юзать rdtsc.
            // В итоге StopWatch удобная обертка над 
        }
    }
}
