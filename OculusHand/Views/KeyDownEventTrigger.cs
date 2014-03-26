using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interactivity = System.Windows.Interactivity;
using System.Windows.Input;
using System.Windows;

namespace OculuSLAM.Views
{
    /// <summary>
    /// 特定のキーが押下された際にイベントを起動するトリガーです。
    /// </summary>
    public class KeyDownEventTrigger : Interactivity.EventTrigger
    {
        /// <summary>
        /// イベントを起動するキーを取得、もしくは設定します。
        /// </summary>
        public Key Key
        {
            get { return (Key)GetValue(EventKeyProperty); }
            set { SetValue(EventKeyProperty, value); }
        }

        /// <summary>
        /// イベントを起動するキーを取得、もしくは設定するための依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty EventKeyProperty =
            DependencyProperty.Register("Key", typeof(Key), typeof(KeyDownEventTrigger), 
            new FrameworkPropertyMetadata(Key.None));

        /// <summary>
        /// このクラスを実体化します。
        /// </summary>
        public KeyDownEventTrigger() : base("KeyDown")
        {
        }

        /// <summary>
        /// KeyDownイベントが発生した際に呼び出されます。特定のキーが押下された場合のみにイベントを起動します。
        /// </summary>
        /// <param name="eventArgs">イベント引数</param>
        protected override void OnEvent(EventArgs eventArgs)
        {
            var e = eventArgs as KeyEventArgs;
            if (e != null && e.Key == Key)
                base.OnEvent(eventArgs);
        }
    }
}
