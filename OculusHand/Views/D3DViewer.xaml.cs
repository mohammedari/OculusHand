using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using _3DTools;
using System.Windows.Media.Media3D;
using OculusHand.ViewModels;
using OculusHand.Models;
using System.ComponentModel;
using OpenCvSharp.CPlusPlus;

namespace OculusHand.Views
{
    /* 
     * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// Viewer.xaml の相互作用ロジック
    /// </summary>
    public partial class D3DViewer : UserControl
    {
        ///////////////////////////////////////////////
        #region 内部変数

        Trackball _trackball;

        #endregion

        ///////////////////////////////////////////////
        #region Public Methods

        /// <summary>
        /// D3DViewerの初期化を行います。
        /// </summary>
        public D3DViewer()
        {
            InitializeComponent();

            _viewModel = new D3DViewerViewModel();

            //Trackballの初期化
            _trackball = new Trackball();
            _trackball.EventSource = mouseCapture;
            mouseCapture.MouseMove += (o, e) =>
                {
                    ViewModel.UpdateMatrix(_trackball.Transform.Value);
                };

            //レンダリングの設定
            CompositionTarget.Rendering += (o, e) => { ViewModel.Render(); };
        }

        #endregion

        ///////////////////////////////////////////////
        #region Properties

        private D3DViewerViewModel _viewModel;

        public D3DViewerViewModel ViewModel
	    {
		    get { return _viewModel;}
		    set { _viewModel = value;}
	    }

        #endregion

        ///////////////////////////////////////////////
        #region Points依存関係プロパティ
        /// <summary>
        /// 表示する点群データです。
        /// </summary>
        [Bindable(true)]
        public Mesh Mesh
        {
            get { return (Mesh)GetValue(MeshProperty); }
            set { SetValue(MeshProperty, value); }
        }

        /// <summary>
        /// 点群データの依存関係プロパティです。値が更新された際に、表示される点群が更新されます。
        /// </summary>
        public static readonly DependencyProperty MeshProperty =
            DependencyProperty.Register(
                "Mesh",
                typeof(Mesh), 
                typeof(D3DViewer), 
                new FrameworkPropertyMetadata(
                    new Mesh(), 
                    FrameworkPropertyMetadataOptions.AffectsRender, 
                    new PropertyChangedCallback(onPointsChanged)
                )
            );

        //依存関係プロパティが更新されると、VMに点群の更新を通知します。
        private static void onPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            D3DViewer viewer = d as D3DViewer;
            if (viewer != null && e.NewValue != null)
                viewer.ViewModel.UpdateMesh((Mesh)e.NewValue);
        }
        #endregion
    }
}