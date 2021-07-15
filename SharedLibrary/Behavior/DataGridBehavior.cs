using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;

namespace SharedLibrary.Behavior
{
    public static class DataGridBehavior
    {
        private static readonly DependencyProperty PreviewDropCommandProperty =
                    DependencyProperty.RegisterAttached
                    (
                        "ScrollCommand",
                        typeof(ICommand),
                        typeof(DataGridBehavior),
                        new PropertyMetadata(ScrollCommandPropertyChangedCallBack)
                    );

        public static void SetScrollCommand(this DataGrid dataGrid, ICommand command)
        {
            dataGrid.SetValue(PreviewDropCommandProperty, command);
        }

        private static ICommand GetScrollCommand(DataGrid dataGrid)
        {
            return (ICommand)dataGrid.GetValue(PreviewDropCommandProperty);
        }

        private static void ScrollCommandPropertyChangedCallBack(
            DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            DataGrid dataGrid = dependencyObject as DataGrid;
            if (null == dataGrid) return;

            if (dataGrid.Items is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += (sender, args) =>
                {
                    //Будем вызывать комманду только в случае добавления и если добавили только 1 элемент (не в случае подзагрузки всей таблицы)
                    if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems.Count == 1)
                    {
                        GetScrollCommand(dataGrid).Execute(dataGrid);
                    }
                };
            }
        }
    }
} 