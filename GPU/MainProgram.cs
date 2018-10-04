using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCLNet;
using CL = OpenCLNet;

namespace GPU
{
    public static class Extend
    {
        /// <summary>
        /// 取指針
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static unsafe IntPtr ToIntPtr(this int[] obj)
        {
            IntPtr PtrA = IntPtr.Zero;
            fixed (int* Ap = obj) return new IntPtr(Ap);
        }
    }
    class MainProgram
    {
        const int mod = 1000;
        #region OpenCL代碼
        private static string CLCode = @"
        __kernel void vector_add_gpu(__global int* src_a, __global int* src_b, __global int* res)
        {
            const int idx = get_global_id(0);
            res[idx] =src_a[idx] + src_b[idx];
        }
        #define max(a,b) ((b)<(a)?(a):(b))
        __kernel void vector_mul_gpu(__global int* src_a, __global int* src_b, __global int* res, __global int* n)
        {
            const int idx = get_global_id(0)+100000*(int)get_global_id(1);//+10000*get_global_id(2);
            //res[idx]=0;
            for(int i=max(0,idx-(n[1]-1));i<=idx&&i<n[0];i++) res[idx] += src_a[i] * src_b[idx-i];
        }

        __kernel void vector_inc_gpu(__global int* src_a, __global int* res)
        {
            const int idx = get_global_id(0);
            res[idx] =src_a[idx] + 1;
        }
        ";
        #endregion
        static Random rand = new Random();
        static int[]CalOnGPU(int[]A,int[]B, Device oclDevice=null)
        {
            if (oclDevice == null)
            {
                //獲取平台數量
                OpenCL.GetPlatformIDs(32, new IntPtr[32], out uint num_platforms);
                var devs = new List<Device>();
                //枚舉所有平台下面的設備(CPU和GPU)
                for (int i = 0; i < num_platforms; i++)
                {
                    //這裏後面有個參數,是Enum,這裏選擇GPU,表示只枚舉GPU,在沒有GPU的電腦上可以選CPU,也可以傳ALL,會把所有設備枚舉出來供選擇
                    devs.AddRange(OpenCL.GetPlatform(i).QueryDevices(DeviceType.GPU));
                }
                //Console.WriteLine($"device count = {devs.Count}");
                //選中運算設備,這裏選第一個其它的釋放掉
                int deviceIndex = devs.Count - 1;
                oclDevice = devs[deviceIndex];
                devs.RemoveAt(deviceIndex);
                for (int i = 0; i < devs.Count; i++) devs[i].Dispose();
            }

            /*Console.WriteLine($"max work group size = {oclDevice.MaxWorkGroupSize}\r\n" +
                $"max work item dimension = {oclDevice.MaxWorkItemDimensions}\r\n" +
                $"max work itme size = {string.Join(", ", oclDevice.MaxWorkItemSizes)}\r\n" +
                $"max mem alloc size = {oclDevice.MaxMemAllocSize}\r\n" +
                $"max parameter size = {oclDevice.MaxParameterSize}");*/
            
            //根據配置創建上下文
            var oclContext = oclDevice.Platform.CreateContext(
                new[] { (IntPtr)ContextProperties.PLATFORM, oclDevice.Platform.PlatformID, IntPtr.Zero, IntPtr.Zero },
                new[] { oclDevice },
                (errInfo, privateInfo, cb, userData) => { },
                IntPtr.Zero
            );
            //創建命令隊列
            var oclCQ = oclContext.CreateCommandQueue(oclDevice, CommandQueueProperties.PROFILING_ENABLE);

            //定義一個字典用來存放所有核
            var Kernels = new Dictionary<string, Kernel>();
            #region 編譯代碼並導出核
            {
                var oclProgram = oclContext.CreateProgramWithSource(CLCode);
                try
                {
                    oclProgram.Build();
                }
                catch (OpenCLBuildException EEE)
                {
                    Console.WriteLine(EEE.BuildLogs[0]);
                    Console.ReadKey(true);
                    throw EEE;
                    //return null;
                }
                foreach (var item in new[] { /*"vector_add_gpu",*/ "vector_mul_gpu", "vector_inc_gpu" })
                {
                    Kernels.Add(item, oclProgram.CreateKernel(item));
                }
                oclProgram.Dispose();
            }
            #endregion

            System.Diagnostics.Trace.Assert(A.Length == B.Length && A.Length % 10 == 0);
            var C = new int[A.Length + B.Length - 1];

            #region 調用vector_add_gpu核
            {
                //Console.Write()
                for (int i = 0; i < C.Length; i++) C[i] = 0;
                var D = new int[] { A.Length ,B.Length};
                //在顯存創建緩衝區並把HOST的數據拷貝過去
                var n1 = oclContext.CreateBuffer(MemFlags.READ_WRITE | MemFlags.COPY_HOST_PTR, A.Length * sizeof(int), A.ToIntPtr());
                var n2 = oclContext.CreateBuffer(MemFlags.READ_WRITE | MemFlags.COPY_HOST_PTR, B.Length * sizeof(int), B.ToIntPtr());
                //還有一個緩衝區用來接收回參
                var n3 = oclContext.CreateBuffer(MemFlags.READ_WRITE | MemFlags.COPY_HOST_PTR, C.Length * sizeof(int), C.ToIntPtr());
                var n4 = oclContext.CreateBuffer(MemFlags.READ_WRITE | MemFlags.COPY_HOST_PTR, D.Length * sizeof(int), D.ToIntPtr());
                //把參數填進Kernel裏
                Kernels["vector_mul_gpu"].SetArg(0, n1);
                Kernels["vector_mul_gpu"].SetArg(1, n2);
                Kernels["vector_mul_gpu"].SetArg(2, n3);
                Kernels["vector_mul_gpu"].SetArg(3, n4);
                //把調用請求添加到隊列裏,參數分別是:Kernel,數據的維度,每個維度的全局工作項ID偏移,每個維度工作項數量(我們這裏有4個數據,所以設為4),每個維度的工作組長度(這裏設為每4個一組)
                //oclCQ.EnqueueNDRangeKernel(Kernels["vector_mul_gpu"], 3, new[] { 0, 0, 0 }, new[] { 100, 100, A.Length * 2 / 10000 }, new[] { 1, 10, 1 });
                oclCQ.EnqueueNDRangeKernel(Kernels["vector_mul_gpu"], 2, new[] { 0, 0 }, new[] { 100000, A.Length * 2 / 100000 }, new[] { 1000, 1 });
                //設置柵欄強制要求上面的命令執行完才繼續下面的命令.
                oclCQ.EnqueueBarrier();
                //oclCQ.Finish();
                //Console.Write("Sleeping...");
                //System.Threading.Thread.Sleep(1000);
                //Console.WriteLine("OK");
                //添加一個讀取數據命令到隊列裏,用來讀取運算結果
                oclCQ.EnqueueReadBuffer(n3, true, 0, C.Length * sizeof(int), C.ToIntPtr());
                //開始執行
                oclCQ.Finish();
                n1.Dispose();
                n2.Dispose();
                n3.Dispose();
                n4.Dispose();
                //C = C;//在這裏打斷點,查看返回值
                Console.WriteLine("OK");
                //Console.WriteLine(string.Join(", ", C));
                //Console.ReadLine();
            }
            // */
            #endregion

            //按順序釋放之前構造的對象
            oclCQ.Dispose();
            oclContext.Dispose();
            oclDevice.Dispose();
            return C;
        }
        static int[]CalOnCPU(int[]A,int[]B)
        {
            var C = new int[A.Length + B.Length - 1];
            for (int i = 0; i < C.Length; i++) C[i] = 0;
            Parallel.For(0, C.Length, idx =>
              {
                  for (int i = Math.Max(0, idx - (B.Length - 1)); i <= idx && i < A.Length; i++) C[idx] += A[i] * B[idx - i];
              });
            return C;
        }
        static void Main(string[] args)
        {
            const int n = 200000;
            var A = new int[n];
            for (int i = 0; i < A.Length; i++) A[i] = 1;
            var B = new int[n];
            for (int i = 0; i < B.Length; i++) B[i] = 1;
            int[] ans2 = null;
            {
                var now = DateTime.Now;
                ans2 = CalOnCPU(A, B);
                Console.WriteLine($"Time on CPU: {DateTime.Now - now}");
            }
            int[] ans1 = null;
            {
                ////獲取平台數量
                //OpenCL.GetPlatformIDs(32, new IntPtr[32], out uint num_platforms);
                //var devs = new List<Device>();
                ////枚舉所有平台下面的設備(CPU和GPU)
                //for (int i = 0; i < num_platforms; i++)
                //{
                //    //這裏後面有個參數,是Enum,這裏選擇GPU,表示只枚舉GPU,在沒有GPU的電腦上可以選CPU,也可以傳ALL,會把所有設備枚舉出來供選擇
                //    devs.AddRange(OpenCL.GetPlatform(i).QueryDevices(DeviceType.GPU));
                //}
                Parallel.For(0, 4, gpuIdx =>
                {
                    for (int i = 0; true || i < 10; i++)
                    {
                        Console.Write($"#{i} ");
                        try
                        {
                            var now = DateTime.Now;
                            ans1 = CalOnGPU(A, B);
                            Console.WriteLine($"Time on GPU#{gpuIdx}: {DateTime.Now - now},\tSame? {ans1.Zip(ans2, (a, b) => a - b).All(a => a == 0)}");
                        }
                        catch (Exception EEE)
                        {
                            Console.WriteLine(EEE.ToString());
                        }
                    }
                });
            }
            //Console.WriteLine(string.Join(",", ans1));
            //Console.WriteLine(string.Join(",", ans2));
            Console.ReadLine();
        }
    }
}
