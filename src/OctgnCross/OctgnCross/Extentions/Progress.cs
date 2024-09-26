using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DialogHostAvalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json.Linq;
using OctgnCross.Controls;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Octgn.Extentions;

public static class Progress
{
    // public static void Run(Action method, string progressTitle = null, string progressInfo = null)
    // {
    //     Run<object>(
    //         delegate
    //         {
    //             method();
    //             return null;
    //         },
    //         progressTitle,
    //         progressInfo
    //     );
    // }

    public static async Task<T> RunAsync<T>(Window owner, Func<Task<T>> operation,Func<Task<T>> completeAction=null, string progressTitle = null,
        string progressInfo = null)
    {
        
        // var dialog = new ProgressWindow();
        // dialog.Message = progressInfo;
        // dialog.OnClose += async ( o, result ) => {
        //     ButtonsEnabled = true;
        //     await completeAction();
        //     dialog.Dispose();
        // };

        // progress.Title = progressTitle ?? "Progress_DefaultTitle".Translate();
        // progress.Info = progressInfo ?? "Progress_DefaultInfo".Translate();

        using var dialog = new WaitingDialog();
        dialog.Message = progressInfo;
        dialog.Title = progressTitle;
        // dialog.OnClose += async ( o, result ) => {
        // 	// ButtonsEnabled = true;
        //     
        // 	
        //     
        // 	dialog.Dispose();
        // };
        // await dialog.Show( DialogPlaceHolder, operation, progressTitle, progressInfo );
        // return dialog;
        
        var task = Task.Run(async () => await operation())
            .ContinueWith(async t =>
                {
                    if (completeAction != null)
                    {
                        await completeAction();    
                    }
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.GetDialogSession(null)?.Close(false);
                    });
                    

                    return await t;
                },
                TaskContinuationOptions.AttachedToParent
            );

        await DialogHost.Show(dialog);

        try
        {
            return task.Result.Result;
        }
        catch (AggregateException ae)
        {
            var ex = ae.Flatten();

            if (ex.InnerException != null)
            {
                throw ex.InnerException;
            }

            throw ex;
        }
    }

    // public static T Run<T>(Window owner, Func<T> operation, string progressTitle = null, string progressInfo = null)
    // {
    //     return RunAsync(owner, () => Task.FromResult<T>(operation()));
    // }

    public class ProgresBarInfo
    {
        private WaitingDialog progressView;
        private int _current;
        private int? _count;
        private string _text;

        public ProgresBarInfo(WaitingDialog progressView)
        {
            this.progressView = progressView;
        }

        public int Current
        {
            get { return _current; }
            set
            {
                _current = value;
                //progressView.CurrentValue = value;
            }
        }

        public int? Count
        {
            get { return _count; }
            set
            {
                _count = value;
                if (_count.HasValue)
                {
                    //progressView.MaximumValue = _count.Value;
                }
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                //progressView.Info = value;
            }
        }
    }

    // public static async Task<T> RunWithProgress<T>(
    //     Window owner,
    //     Func<ProgresBarInfo, T> operation,
    //     int maximum,
    //     string progressTitle = null,
    //     string progressInfo = null
    // )
    // {
    //     var dialog = new ProgressWindow();
    //     dialog.IsIndetermined = false;
    //     // progress.Title = progressTitle ?? "Progress_DefaultTitle".Translate();
    //     // progress.Info = progressInfo ?? "Progress_DefaultInfo".Translate();
    //     dialog.MaximumValue = maximum;
    //
    //     ProgresBarInfo info = new ProgresBarInfo(dialog);
    //
    //     var task = Task.Run(() => { return operation(info); })
    //         .ContinueWith(
    //             t =>
    //             {
    //                 Dispatcher.UIThread.Invoke(dialog.Close);
    //
    //                 return t;
    //             },
    //             TaskContinuationOptions.AttachedToParent
    //         );
    //
    //     await dialog.ShowDialog(owner);
    //
    //     try
    //     {
    //         return task.Result.Result;
    //     }
    //     catch (AggregateException ae)
    //     {
    //         var ex = ae.Flatten();
    //
    //         if (ex.InnerException != null)
    //         {
    //             throw ex.InnerException;
    //         }
    //
    //         throw ex;
    //     }
    // }


}