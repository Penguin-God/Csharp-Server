using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ServerCore
{
    class Program
    {
        // 쓰레드 관련 내용
        class Study_Thread
        {
            void Start_NewThread()
            {
                while (true)
                    Console.WriteLine("Hello Thread!!");
            }

            // ThreadPool에 넣으려면 파라미터로 object를 하나 가지고 있어야 함
            void Start_PoolThread(object state)
            {
                Console.WriteLine("Hello Thread!!");
            }

            void Main(string[] args)
            {
                // 쓰레드 풀은 오브젝트 풀링과 개념이 비슷함
                // 풀에 미리 생성되 있는 쓰레드를 가져와 작업을 부여하고 일을 마치면 다시 풀에 돌아가서 대기함
                // 풀에 있는 쓰레드가 제한되어 있으므로 미친듯이 많이 사용하면 이전에 쓰레드가 일을 마칠 때까지 대기 후 일을 배정함
                // 쓰레드를 새로 만드는 것은 오브젝트를 새로 만드는 것과 비슷하며 부담이 많이 듬
                // 부를때도 쓰레드풀링 이라고 부름
                ThreadPool.QueueUserWorkItem(Start_PoolThread);

                ThreadPool.SetMinThreads(1, 1); // 최소 쓰레드 1개
                ThreadPool.SetMaxThreads(5, 5); // 풀에서 최대로 사용 가능한 쓰레드 5개로 설정

                // Thread가 동작할 함수를 넘김
                // C#의 쓰레드는 기본적으로 forground 쓰레드
                // forground일 경우 Main함수가 꺼져도 동작함
                // forground가 아니라 Background일 경우 BackGround가 꺼지면 같이 꺼짐
                Thread thread_1 = new Thread(Start_NewThread);
                thread_1.Name = "My Thread";
                thread_1.Start();
                //thread_1.IsBackground = true;
                thread_1.Join(); // 쓰레드가 멈출 때까지 대기하는 함수

                // 쓰레드풀에서 갖다 쓰는거긴 하지만 특정 옵션을 추가해 별도의 쓰레드로 사용가능 등 여러가지 짓거리 가능
                // LongRunning 옵션을 넣어서 얘는 오래 걸리니 별도다. 라고 선언 근데 옵션 안넣으면 그냥 하는거랑 똑같음
                Task task_1 = new Task(() => { while (true) { } }, TaskCreationOptions.LongRunning);


                // Main함수 내에서Start_Thread()를 호출했다면 무한 루프에 막혀서 아래 코드가 실행되지 않음
                // 하지만 쓰레드를 따로 생성해서 실행하므로써 두개의 while문이 동시에 돌아감
                while (true)
                    Console.WriteLine("Hello World");
            }
        }

        // 컴파일러 주의사항 관련 내용
        class Compile
        {
            // 평소에는 Debug모드로 돌리지만 프로그램을 빌드할 때는 Release 모드로 출시함
            // 이때 컴파일러 최적화를 진행하는데 멀티 쓰레드를 사용할 때 문제가 되기도 함
            // static 변수를 선언해서 다른 곳에서 바꾸도록 했는데 그딴거 신경안쓰고 지 맘대로 최적화해서 코드의 의미가 달라질 수 있음

            // volatile : 컴파일러 최적화를 막는 문법(안쓰는걸 추천하는 문법)
            volatile static bool isStop = false;

            void TaskMain()
            {
                Console.WriteLine("Hello Task");
                while (!isStop)
                {
                    Console.WriteLine("Looping");
                    Thread.SpinWait(50);
                }
                Console.WriteLine("Bye Task");
            }

            void Main(string[] args)
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

        // 캐시 이론 및 테스트
        class Cash
        {
            // Cash가 있는 이유
            // 우선 CPU와 메모리 사이에는 어느 정도 물리적으로 거리가 있음
            // 때문에 변수 등의 값이 바뀐다고 일일이 메모리에 가서 데이터를 기입하는 건 비효율적
            // 이 문제를 해결하기 위한 방안으로 코어에는 연산을 하는 ALU와 기억을 하는 캐시 장치가 있는데
            // 캐시 장치에 데이터를 꾹꾹 모아두다가 어느 정도 시간이 지난 후에 메모리에 가서 저장함
            // 만약 메모리에 전달하기 전에 값이 바뀐다면 그냥 바뀐 값을 전달하면 된다.
            // 하지만 바로바로 메모리에 전달했다면 바꿘 데이터를 또 메모리에 가서 저장해야된다. 즉 한 번 갈걸 두 번 간다. 개손해다.
            // 이렇게 관련 데이터들을 캐시 장치에 저장해둘때는 2가지의 철학이 있다.
            // 1. 방금 접근한 메모리는 또 접근할 확률이 높으니 저장해둔다
            // 2. 방금 접근한 메모리와 근접한 주솟값에 있는 애들은 접근할 확률이 높으니 저장해 둔다( 예를 들어 배열의 주솟값은 메모리에 순서대로 쭈르륵 저장된다. )

            public void Main()
            {
                int[,] arr = new int[10000, 10000];

                long start = 0;
                long end = 0;
                // [0][1][2][3][4] [5][6][7]....[][] [][][][][] [][][][][] [][][][][]
                start = DateTime.Now.Ticks;
                for (int x = 0; x < 10000; x++)
                {
                    for (int y = 0; y < 10000; y++)
                        arr[x, y] = 1;
                }
                end = DateTime.Now.Ticks;
                Console.WriteLine($"걸린 시간 틱 : {end - start}"); // 4881071

                // [0][5]...[][][] [1][6][][][] [2][7][][][] [3][8][][][] [4][9][][][]
                start = DateTime.Now.Ticks;
                for (int x = 0; x < 10000; x++)
                {
                    for (int y = 0; y < 10000; y++)
                        arr[y, x] = 1;
                }
                end = DateTime.Now.Ticks;
                Console.WriteLine($"걸린 시간 틱 : {end - start}"); //  8215630

                // 시간 차이가 나는 이유 
                // 만약 배열이 5 * 5 였다면 실행순서는 반복문 위의 주석과 같다
                // 캐시의 2번 철학에 따라 근접한 주소를 가진 메모리들을 저장해두는데
                // 2번째 반목문을 저 멀리 주소의 메모리에 접근해서 캐시의 효용성을 보기가 힘들다. 그래서 느리다.
            }
        }

        // 메모리 베리어
        class MemoryBarrier
        {
            int x = 0;
            int y = 0;
            int r1 = 0;
            int r2 = 0;

            void Task1()
            {
                x = 1;
                r1 = x;
            }

            void Task2()
            {
                y = 1;
                r2 = y;
            }

            public void Main()
            {
                int count = 0;
                while (true)
                {
                    count++;
                    x = y = r1 = r2 = 0;
                    Task task1 = new Task(Task1);
                    Task task2 = new Task(Task2);
                    task1.Start();
                    task2.Start();

                    Task.WaitAll(task1, task2);

                    if (r1 == 0 && r2 == 0) break;
                }

                Console.WriteLine($"{count}번만에 탈출!!");
            }
        }

        static void Main(string[] args)
        {
            MemoryBarrier memoryBarrier = new MemoryBarrier();
            memoryBarrier.Main();
        }
    }    
}
