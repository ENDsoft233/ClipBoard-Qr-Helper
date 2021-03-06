﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using ToastCore.Notification;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using ZXing;
using System.Drawing;
using System.IO;

namespace ToastHelper {
    public static class Common // static 不是必须
    {
        private static bool _IsPausingScan = false;
        public static bool IsPausingScan
        {
            get { return _IsPausingScan; }
            set { _IsPausingScan = value; }
        }

        private static bool _IsSilence = false;
        public static bool IsSilence
        {
            get { return _IsSilence; }
            set { _IsSilence = value; }
        }
    }

    public partial class MainWindow : Window {
        private ToastManager _manager;
        private Action _notify;
        private readonly IRunningObjectTable rot;

        [DllImport("Ole32.dll")]
        static extern int CreateClassMoniker([In] ref Guid rclsid,
            out IMoniker ppmk);

        [DllImport("Ole32.dll")]
        public static extern void GetRunningObjectTable(
           int reserved,
           out IRunningObjectTable pprot);

        [DllImport("Ole32.dll")]
        static extern int CoRegisterClassObject(
           [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
           [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
           uint dwClsContext,
           uint flags,
           out uint lpdwRegister);

        public MainWindow() {
            InitializeComponent();
            _manager = new ToastManager();
            _manager.Init<ToastManager>("ClipBoard Qr Helper");
            ToastManager.ToastCallback += ToastManager_ToastCallback;
            GetRunningObjectTable(0, out this.rot);
            if (Common.IsSilence == false)
            {
                _notify = new Action(() => _manager.Notify("欢迎使用，我在托盘为你服务哦！", "ClipBoard Qr Helper")); ; ;
                _notify?.BeginInvoke(null, null);
            }
        }

        private void ToastManager_ToastCallback(string app, string arg, List<KeyValuePair<string, string>> kvs) {
            App.Current.Dispatcher.Invoke(() => {
                if (arg.StartsWith("copy:")) Clipboard.SetText (arg.Substring(5));
                if (arg.StartsWith("goUrl:")) Process.Start(arg.Substring(6));
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;
            this.Visibility = Visibility.Hidden;
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage() && Common.IsPausingScan == false)
            {
                Bitmap image = null;
                try { image = BitmapFromSource(Clipboard.GetImage()); } catch {
                    try { image = (Bitmap)Image.FromFile(Clipboard.GetFileDropList()[0]); } catch
                    {
                        image = null;
                    }
                }
                if (image != null)
                {
                    IBarcodeReader reader = new BarcodeReader();
                    var barcodeBitmap = image;
                    var result = reader.Decode(barcodeBitmap);
                    if (result != null)
                    {
                        if (result.Text.ToLower().StartsWith("http://") || result.Text.ToLower().StartsWith("https://"))
                        {
                            _notify = new Action(() => _manager.Notify("从剪贴板图片中读取到了二维码信息", result.Text, new ToastCommands[] { new ToastCommands { Content = "复制", Argument = "copy:" + result.Text } }, new ToastCommands[] { new ToastCommands { Content = "前往", Argument = "goUrl:" + result.Text } })); ; ;
                        }
                        else
                        {
                            _notify = new Action(() => _manager.Notify("从剪贴板图片中读取到了二维码信息", result.Text, new ToastCommands[] { new ToastCommands { Content = "复制", Argument = "copy:" + result.Text } }, new ToastCommands[] { new ToastCommands { Content = "忽略", Argument = "nothing" } })); ; ;
                        }
                        _notify?.BeginInvoke(null, null);
                    }
                }
            }
        }

        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
