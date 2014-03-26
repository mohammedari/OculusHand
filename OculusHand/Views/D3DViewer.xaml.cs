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
using OculuSLAM.ViewModels;
using OculuSLAM.Models;
using System.ComponentModel;
using OpenCvSharp.CPlusPlus;

namespace OculuSLAM.Views
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
        public PointCloud Points
        {
            get { return (PointCloud)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        /// <summary>
        /// 点群データの依存関係プロパティです。値が更新された際に、表示される点群が更新されます。
        /// </summary>
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register(
                "Points",
                typeof(PointCloud), 
                typeof(D3DViewer), 
                new FrameworkPropertyMetadata(
                    new PointCloud(), 
                    FrameworkPropertyMetadataOptions.AffectsRender, 
                    new PropertyChangedCallback(onPointsChanged)
                )
            );

        //依存関係プロパティが更新されると、VMに点群の更新を通知します。
        private static void onPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            D3DViewer viewer = d as D3DViewer;
            if (viewer != null && e.NewValue != null)
                viewer.ViewModel.UpdatePoints((PointCloud)e.NewValue);
        }
        #endregion

        #region Transform依存関係プロパティ
        /// <summary>
        /// カメラの位置姿勢です。
        /// </summary>
        [Bindable(true)]
        public Mat Transform
        {
            get { return (Mat)GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        /// <summary>
        /// カメラの位置姿勢の依存関係プロパティです。値が更新された際に、表示されるカメラ座標が更新されます。
        /// </summary>
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                "Transform", 
                typeof(Mat), 
                typeof(D3DViewer), 
                new FrameworkPropertyMetadata(
                    Mat.Eye(4, 4, MatType.CV_32F).ToMat(), 
                    FrameworkPropertyMetadataOptions.AffectsRender, 
                    new PropertyChangedCallback(onTransformChanged)));

        //依存関係プロパティが更新されると、VMに位置姿勢の更新を通知します。
        private static void onTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            D3DViewer viewer = d as D3DViewer;
            if (viewer != null && e.NewValue != null)
                viewer.ViewModel.UpdateTransform((Mat)e.NewValue);
        }
        #endregion
    }
}