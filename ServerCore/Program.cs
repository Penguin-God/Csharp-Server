using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ServerCore
{
    class Program
    {
        // 평소에는 Debug모드로 돌리지만 프로그램을 빌드할 때는 Release 모드로 출시함
        // 이때 컴파일러 최적화를 진행하는데 멀티 쓰레드를 사용할 때 문제가 되기도 함
        // static 변수를 선언해서 다른 곳에서 바꾸도록 했는데 그딴거 신경안쓰고 지 맘대로 최적화해서 코드의 의미가 달라질 수 있음

        // volatile : 컴파일러 최적화를 막는 문법(안쓰는걸 추천하는 문법)
        volatile static bool isStop = false;

        static void TaskMain()
        {
            Console.WriteLine("Hello Task");
            while (!isStop)
            {
                Console.WriteLine("Looping");
                Thread.SpinWait(50);
            }
            Console.WriteLine("Bye Task");
        }

        static void Main(string[] args)
        {
            Task t = new Task(TaskMain);
            t.Start();

            Thread.Sleep(1000); // 1초 대기 (단위는 밀리 세컨드)

            Console.WriteLine("멈춰!!");
            isStop = true;

            t.Wait(); // t 쓰레드가 멈출 때까지 대기

            Console.WriteLine("멈.췄.다.");
        }
    }
}
