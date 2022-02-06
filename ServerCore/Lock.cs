using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerCore
{
    class Lock
    {
        // Lock을 쓰는 이유 
        // 기본적으로 멀티 쓰레드는 빠르니까 쓴다.
        // 게임 서버를 구현할 때 싱글 쓰레드로 하면 준내 느리니까
        // 근데 멀티 쓰레드를 쓰다가 여러 쓰레드가 특정 자원을 동시에 쓴다(class, 함수, 변수)
        // 이러면 변수 값이 이상한 값이 되고 잘못하면 서버가 터진다
        // 그래서 상호배재하여 멀티쓰레드지만 특정 부분이 싱글처럼 돌아가게 하기 위해 쓴다.(물론 대가로 속도를 지불한다.)

        class Study_Interlocked
        {
            // atomic = 원자성
            // 원자는 물리적으로 쪼개지지 않는 것으로 원자성은 더 이상 쪼개지면 안되는 작업이 가지는 특성을 뜻함

            // ex) 집행검 교환 
            // 1. 소유자 인벤에서 템 삭제
            // 2. 비소유자 인벤에 템 추가
            // 근데 원자성이 보장되지 않고 1번만 성공하고 2번이 실패하면? 1억 삭제됨
            // 아래에서도 0이 출력되지 않는 이유는 ++, --가 어셈블리어 쪽으로 들어가면
            // 주소에 접근 후 값을 저장해서 임시 값을 만들고 임시값에 실제로 값을 더하고 다시 대입하는
            // 총 3단계에 작업으로 진행되 원자성이 보장되지 않는데, 그걸 멀티 쓰레드 환경으로 뒤죽박죽 실행했기 때문

            // 더 자세히 설명하자면
            // Thread1이 먼저 실행됐다고 가정하고 각각 임시값 생성, 임시값 증가, 실제 값에 임시값 대입으로 3단계로 나눈면
            // 3단계가 전부 끝나고 Thread2가 실행되야 하는데 Thread1 실행 중에 Thread2에서 값 대입하고 빼고 별 지랄을 다하기 때문에 값이 이상해짐
            // 그리고 정말 다행히 원자성을 보장해주는 문법이 있음. 그러면 먼저 실행된 작업이 끝나기 전까지 기다리기 때문에 의도대로 잘 작동함
            // Interlocked문법을 이용해 연산을 하면 기존 3단계가 한 번에 실행되고, 순서도 보장받음 
            // 물론 number에 대한 작업이 끝날 때까지 다른 애들이 기다려야되서 느림. 기존에 캐시 이론이 쓰잘데기가 없어짐.

            int number = 0;

            void Thread1()
            {
                for (int i = 0; i < 100000; i++)
                {
                    // 원자성을 가지고 있는 ++을 실행
                    Interlocked.Increment(ref number);
                    //number++;
                }
            }

            void Thread2()
            {
                for (int i = 0; i < 100000; i++)
                {
                    Interlocked.Decrement(ref number);
                    //number--;
                }
            }

            public void Main()
            {
                Task t1 = new Task(Thread1);
                Task t2 = new Task(Thread2);
                t1.Start();
                t2.Start();
                Task.WaitAll(t1, t2);
                Console.WriteLine(number); // 0이 아닌 값이 나옴
            }
        }

        class Study_Lock
        {
            object obj = new object();
            object user_obj = new object();
            object session_obj = new object();
            int number = 0;

            // 철학 : 상호배재. 단비꺼야.
            // 아래 4개의 함수가 서로 다른 object(자물쇠)로 lock을 걸어 동시에 접근을 막고 서로 꼬이게 되면서 DeadLock 걸림
            // 해결 방법 : 그냥 크래쉬 내고 디버그 모드에서 콜스택 찾아갖고 열심히 고쳐봐
            // 동시에 락을 걸어야 하는 문제기 때문에 시간차로 실행하면 되긴 함. 근데 실제 서버 열고 유저 몰리면? 펑펑 터짐. 그러니 구조를 잘 만들자
            // 고유 id 같은걸 부여해서 디버그 때 추적을 편하게 만들어 놓기도 함
            void TestSession()
            {
                lock (session_obj)
                {
                    // 코드
                }
            }

            void TestUser()
            {
                lock (user_obj)
                {
                    // 코드
                }
            }

            void Test_1()
            {
                lock (session_obj)
                {
                    TestUser();
                }
            }

            void Test_2()
            {
                lock (user_obj)
                {
                    TestSession();
                }
            }

            void Thread1()
            {
                for (int i = 0; i < 100000; i++)
                {
                    Test_1();
                }
            }

            void Thread2()
            {
                for (int i = 0; i < 100000; i++)
                {
                    Test_2();
                    //lock (obj)
                    //{
                    //    number--;
                    //}
                    //Monitor.Enter(obj);
                    //number--;
                    //Monitor.Exit(obj);
                }
            }

            public void Main()
            {
                Task t1 = new Task(Thread1);
                Task t2 = new Task(Thread2);
                t1.Start();
                Thread.Sleep(100); // 임시방편 동시에 일어나면 DeadLock
                t2.Start();
                Task.WaitAll(t1, t2);
                Console.WriteLine(number); // 0이 아닌 값이 나옴
            }
        }

        class SpinLock
        {
            volatile int isLocked = 0;
            // Acquire : 얻다, 습득하다
            void Acquire()
            {
                while (true)
                {
                    // isLocked를 1로 바꿈 그리고 바꾸기 전에 값을 return함
                    //int _original = Interlocked.Exchange(ref isLocked, 1);
                    // 만약 _original이 1이라면 다른 쓰레드에서 접근해서 작업중이기 때문에 다음 루프로 넘김
                    // 저기서 뱉는 _original은 멀티 쓰레드에서 개나소나 접근하는 값이 아니라 싱글 쓰레드에서 돌아가기 때문에 비교문에 사용해도 됨
                    // 싱글 쓰레드라면 값을 바꾸고 대입하는게 서로 다른 작업이겠지만 Exchange는 원자성을 지키며 두 작업을 한번에 다함
                    //if (_original == 0) break;

                    // 근데 놀랍게도 비교도 자체적으로 해주는게 있음 비교도 해주니까 당연히 더 안전함
                    int _expected = 0; // 예상한 값
                    int _desired = 1; // 바뀌기를 원하는 값
                    // isLocked과 _expected가 같으면 _desired 을 isLocked에 넣어줌, 그리고 원본값을 리턴함
                    if (Interlocked.CompareExchange(ref isLocked, _desired, _expected) == _expected) break;

                    // while무한반복문을 쓰면 프로그램이 터질 우려가 있어서 쉬기
                    Thread.Sleep(1); // 1ms 휴식(unity에서 yield return null; 하고 비슷한 느낌)
                    //Thread.Sleep(0); // 휴식이 아닌 다른 쓰레드로 작업을 양도 대신 자신보다 우선순위가 낮을 애들만
                    //Thread.Yield(); // 다른 쓰레드로 무조건 양도
                    // 다른 쓰레드에 작업을 양도하는 것은 꽤나 부하가 크다. 경우에 따라 그냥 무한 반복하는게 나을수도 있음
                }
            }

            // Release : 개봉하다, 공개하다, 출시하다
            void Release()
            {
                isLocked = 0;
            }

            // 0이 안나오는 이유
            // 스핀이 해제될때까지 대기 후 주도권을 가져오는데 멀티 쓰레드라서 bool변수는 하나지만 접근은 여러 곳에서 해댐
            // 그러다가 거의 동시에 접근해서 주도권을 같이 가져옴 => 폭망
            // 해결방벙 : 주도권을 가져올때 다른곳에서 접근했는지 확인 후 아닐때만 가져옴
            int number = 0;
            void Thread1()
            {
                for (int i = 0; i < 10000; i++)
                {
                    Acquire();
                    number++;
                    Release();
                }
            }

            void Thread2()
            {
                for (int i = 0; i < 10000; i++)
                {
                    Acquire();
                    number--;
                    Release();
                }
            }

            public void Main()
            {
                Task t1 = new Task(Thread1);
                Task t2 = new Task(Thread2);
                t1.Start();
                t2.Start();
                Task.WaitAll(t1, t2);
                Console.WriteLine(number);
            }
        }

        class EventLock
        {
            // available : 사용 가능한
            // 인자값으로 bool 타입을 받음 true는 열려있는 상태로 false는 닫혀 있는 상태로 생성
            // 이름에 Auto가 있는 것처럼 문을 열 경우 알아서 닫히고 알아서 열림
            // 특 : 커널 레벨까지 가서 신호를 주고 바꾸고 해서 준내 느림
            AutoResetEvent avaialable = new AutoResetEvent(true);

            //ManualResetEvent manualResetEvent = new ManualResetEvent(true);
            // 입장 후 문 닫는 작업을 따로 해줘야 함. 원자적으로 실행되지 않아서 지금처럼 왔다리 갔다리 할때는 에러가 생길 수 있음
            // 패키지 다운로드, 레벨 로딩과 같은 무거운 작업 후 다른 쓰레드들에게 쫙 열어줄 때 사용

            //Mutex mutex = new Mutex();
            // 위에 클래스들은 bool로 잠금 여부만 따지는데 여기는 int로 2중, 3중 잠금도 할 수 있고 쓰레드ID도 가지고 있어서 비교도 가능함
            // 하지만 역시 느림. 그리고 왠만하면 위에걸로 커버 되서 잘 안씀

            void Acquire()
            {
                avaialable.WaitOne(); // 입장 시도(입장 후 알아서 닫힘)
                // avaialable.Reset(); // 문을 닫는 행동 (위에서 원자적으로 자동 실행됨)
            }

            void Release()
            {
                avaialable.Set(); // 문 열어줌
            }

            int number = 0;
            void Thread1()
            {
                for (int i = 0; i < 10000; i++)
                {
                    Acquire();
                    number++;
                    Release();
                }
            }

            void Thread2()
            {
                for (int i = 0; i < 10000; i++)
                {
                    Acquire();
                    number--;
                    Release();
                }
            }

            public void Main()
            {
                Task t1 = new Task(Thread1);
                Task t2 = new Task(Thread2);
                t1.Start();
                t2.Start();
                Task.WaitAll(t1, t2);
                Console.WriteLine(number);
            }
        }

        // RWLock 구현 및 지금까지 했던거 복습
        public LockProject lockProject = new LockProject();
        public class LockProject
        {
            // RWLock 정책
            // 재귀적 락 허용(Write를 사용중인 쓰레드가 또 Write를 요청한다면 받아줄 것인가?) : Yes
            // Write->Write (OK), Wirte->Read (OK), Read->Write(NO) 
            // 경합 중 5000번 반복 시 yield

            // 32비트를 이용해서 쓰레드의 상황을 확인하는 플래그
            // [Unused(1)] : 마지막은 음수가 될 수 있기 때문에 사용 안함
            // [WriteThreadId(15)] [ReadThreadCount(16)] : 몇개가 Read중인지 체크
            // Write : 하나의 쓰레드만 들어갈 수 있으며 VIP취급으로 Read중인 애들은 끝날때까지 대기
            // Read : Write가 없다면 여러 쓰레드가 lock이 없는 것처럼 자유롭게 사용 가능. Write가 있다면 대기

            // 2진수 1111은 16진수 F
            const int empty_Falg = 0x000000;
            // 2진수 0(8)1(4)1(2)1(1) = 16진수 7
            const int writer_Mask = 0x7FFF0000;
            const int read_Mask = 0x0000FFFF;
            const int maxSpinCount = 5000;

            int _flag = empty_Falg;

            // 상호배재한 상태에서 안전성이 보장되므로 일반 변수처럼 사용해도 됨
            int writeCount = 0;
            public void WriteLock()
            {
                // 재귀적 락 쓰던놈이 또 쓸 때
                int _flagWirterThreadId = (_flag & writer_Mask) >> 16;
                if(_flagWirterThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    writeCount++;
                    return;
                }

                // 쓰레드의 아이디를 받고 아이디를 담기로 정한 위치로 밀어버림
                // 혹시 크기가 15비트를 초과할 때를 대비해 writer_Mask 와 and연산을 해서 0으로 밀어버림
                // 어 그럼 ReadCount가 있을 수도 있다고요? 쓰레드 개수가 1억개가 넘겠냐 2500년 쯤에 가능할듯
                int _writerId = (Thread.CurrentThread.ManagedThreadId << 16) & writer_Mask;
                while (true)
                {
                    // 아무도 WriteLock or ReadLock을 소유중이지 않을 때, 경합해서 소유권을 얻는다.
                    for (int i = 0; i < maxSpinCount; i++)
                    {
                        // 이건 원자성 보장이 안되므로 상당히 위험한 코드
                        // if (_flag == empty_Falg) _flag = _writerId;

                        // 조건문 안의 내용은 위의 줄과 행동은 똑같지만 원자성이 보장되는 코드

                        if (Interlocked.CompareExchange(ref _flag, _writerId, empty_Falg) == empty_Falg)
                        {
                            writeCount = 1;
                            return; // 성공 시 끝(위 함수의 return값은 기존의 _flag값 즉 비어있었는지 확인하는 조건)
                        }
                    }

                    // 실패시 양보
                    Thread.Yield();
                }
            }

            public void WriteUnLock()
            {
                // 나갔으니 _flag 비우기
                int lockCount = --writeCount;
                if (lockCount == 0) Interlocked.Exchange(ref _flag, empty_Falg);
            }

            // Read는 Write가 있을 때 대기하고 없으면 병렬적으로 실행되므로 상호배재같은 건 없음. 특정 자원 동시에 쓰면 망함
            public void ReadLock()
            {
                // WirteLock의 소유자가 없다면 ReadCount를 1 늘린다.

                // WirteLock 쓰던 놈이 ReadLock 요청 시
                // 이렇게 들어올 경우 Read먼저 풀고 Write풀어야 됨. 스택 같은 느낌으로다가
                int _flagWirterThreadId = (_flag & writer_Mask) >> 16;
                if (_flagWirterThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    Interlocked.Increment(ref _flag);
                    return;
                }

                while (true)
                {
                    for (int i = 0; i < maxSpinCount; i++)
                    {
                        // 내가 예상한 _flag 값 정확히는 주도권을 줄 수 있는 원하는 값 = Write가 비어있는 값
                        // 그걸 확인하기 위해서는 현재 _flag에서 Wirte부분을 다 0으로 만들어도 기존 _flag와 같아야함
                        // 그래서 & 연산으로 wirte부분을 밀어버린 _expected 변수를 선언 후 _flag와 비교
                        // 그리고 CompareExchange 사용 후 통과 시 +1을 함(ReadCount는 비트 끝부분이므로 별도의 비트 연산 필요 X)
                        int _expected = (_flag & read_Mask);
                        // 이때 _expected 구하는 과정과 조건문은 원자성이 보장되지 않아 두개 이상의 쓰레드가 동시에 들어올 시
                        // 승자가 먼저 _flag의 값을 바꾸고 나머지는 바꿘 _flag 의 값과 기존에 구했던 _expected의 값이 다르므로
                        // return이 실행되지 않고 다음 루프에서 다시 ReadCount를 올리면 된다.
                        // ReadCount부분의 비트값만 에러가 없으면 되므로별 문제 없음.
                        if (Interlocked.CompareExchange(ref _flag, _expected + 1, _expected) == _expected) return;

                        // writer_Mask는 지정 위치가 1로 채워져 있으므로 _flag의 writer비트 부분이 모두 꺼져있어야 0이됨
                        //if ((_flag & writer_Mask) == 0) return; // 원자성 미보장 때문에 안씀
                    }
                }
            }

            public void ReadUnLock()
            {
                Interlocked.Decrement(ref _flag);
            }
        }
    }
}
