using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Windows11Requirement.ViewModels
{
    /// <summary>
    /// ハードウエア要件/仕様の最小要件
    /// https://www.microsoft.com/ja-jp/windows/windows-11-specifications
    /// https://pc.watch.impress.co.jp/docs/column/ubiq/1334310.html
    /// </summary>
    class MainWindowViewModel : BindableBase
    {
        public ReactiveProperty<string> Os { get; } = new ReactiveProperty<string>("OS: " + RuntimeInformation.OSDescription);

        /// <summary>
        /// プロセッサ
        /// 1 ギガヘルツ (GHz) 以上で 2 コア以上の64 ビット互換プロセッサまたは System on a Chip (SoC)
        /// </summary>
        public ReactiveProperty<string> Processor { get; }

        /// <summary>
        /// RAM
        /// 4 ギガバイト (GB)
        /// </summary>
        public ReactiveProperty<string> Ram { get; }

        /// <summary>
        /// ストレージ
        /// 64 GB 以上の記憶装置
        /// 注意: 詳細は下記の「Windows 11 を最新状態に維持するために必要な空き領域についての詳細情報」をご覧ください。
        /// </summary>
        public ReactiveProperty<string> Storage { get; }

        /// <summary>
        /// システム ファームウェア
        /// UEFI、セキュア ブート対応
        /// </summary>
        public ReactiveProperty<string> Farmware { get; } = new ReactiveProperty<string>("Farmware");

        /// <summary>
        /// TPM
        /// トラステッド プラットフォーム モジュール (TPM) バージョン 2.0
        /// </summary>
        public ReactiveProperty<string> Tpm { get; } = new ReactiveProperty<string>("TPM");

        /// <summary>
        /// グラフィックス カード
        /// DirectX 12 以上 (WDDM 2.0 ドライバー) に対応
        /// </summary>
        public ReactiveProperty<string> GraphicsCard { get; }

        /// <summary>
        /// ディスプレイ
        /// 対角サイズ 9 インチ以上で 8 ビット カラーの高解像度 (720p) ディスプレイ
        /// </summary>
        public ReactiveProperty<string> Display { get; } = new ReactiveProperty<string>($"Display: {Screen.PrimaryScreen.Bounds.Width}x{Screen.PrimaryScreen.Bounds.Height}");

        /// <summary>
        /// インターネット接続と Microsoft アカウント
        /// Windows 11 Home Edition を初めて使用するとき、デバイスのセットアップを完了するには、インターネット接続とMicrosoft アカウントが必要です。
        /// Windows 11 Home の S モードを解除する場合もインターネット接続が必要です。S モードの詳細はこちらをご覧ください。
        /// すべての Windows 11 Edition について、更新プログラムのインストールや一部の機能のダウンロードと使用にはインターネット アクセスが必要です。
        /// </summary>
        public ReactiveProperty<string> Internet { get; } = new ReactiveProperty<string>($"Internet: {NetworkInterface.GetIsNetworkAvailable()}");

        public MainWindowViewModel()
        {
            var searcher = new ManagementObjectSearcher("select MaxClockSpeed from Win32_Processor");
            uint clockSpeed = 0;
            foreach (var item in searcher.Get())
            {
                clockSpeed = (uint)item["MaxClockSpeed"];
            }

            var bit = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";

            this.Processor = new ReactiveProperty<string>($"Processor: {clockSpeed / 1000f:F1}GHz {Environment.ProcessorCount}コア {bit}");

            var mc = new ManagementClass("Win32_OperatingSystem");
            using var moc = mc.GetInstances();
            ulong memorySize = 0;
            foreach (var mo in moc)
            {
                memorySize = (ulong)mo["TotalVisibleMemorySize"];
            }
            this.Ram = new ReactiveProperty<string>($"RAM: {memorySize / 1024f / 1024f:#,0}GB");

            // https://ufcpp.net/study/powershell/interop.html
            // https://tech.tanaka733.net/entry/2013/12/10/powershell-from-csharp
            //var result = PowerShell.Create()
            //    .AddCommand("$(Get-ComputerInfo).BiosFirmwareType")
            //    .Invoke();
            //this.Farmware = new ReactiveProperty<string>("Farmware: {result.First().ToString()}");

            long space = 0;
            foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady))
            {
                space = Math.Max(space, drive.TotalFreeSpace);
            }
            this.Storage = new ReactiveProperty<string>($"Storage: {space / 1024f / 1024f / 1024f:#,0}GB");

            this.GraphicsCard = new ReactiveProperty<string>($"GraphicsCard: DirectX {this.checkdxversion_dxdiag()}");

            // TPM
            // WinRT: https://docs.microsoft.com/ja-jp/uwp/api/windows.system.profile.systemidentification.getsystemidforpublisher?view=winrt-19041
            // SystemIdentification.GetSystemIdForPublisher()
        }

        // https://stackoverflow.com/questions/6159850/how-to-code-to-get-direct-x-version-on-my-machine-in-c
        private int checkdxversion_dxdiag()
        {
            Process.Start("dxdiag", "/x dxv.xml");
            while (!File.Exists("dxv.xml"))
                Thread.Sleep(1000);
            XmlDocument doc = new XmlDocument();
            doc.Load("dxv.xml");
            XmlNode dxd = doc.SelectSingleNode("//DxDiag");
            XmlNode dxv = dxd.SelectSingleNode("//DirectXVersion");

            return Convert.ToInt32(dxv.InnerText.Split(' ')[1]);
        }
    }
}
