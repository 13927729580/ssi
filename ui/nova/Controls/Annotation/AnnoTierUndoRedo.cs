using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    #region UndoRedo

    internal interface IUndoRedo
    {
        void Undo(int level);

        void Redo(int level);

        void InsertObjectforUndoRedo(ChangeRepresentationObject dataobject);
    }

    public partial class AnnoTierUndoRedo : IUndoRedo
    {
        private Stack<ChangeRepresentationObject> _UndoActionsCollection = new Stack<ChangeRepresentationObject>();
        private Stack<ChangeRepresentationObject> _RedoActionsCollection = new Stack<ChangeRepresentationObject>();

        public event EventHandler EnableDisableUndoRedoFeature;

        private Canvas _Container;

        public Canvas Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        #region IUndoRedo Members

        public void Undo(int level)
        {
            for (int i = 1; i <= level; i++)
            {
                if (_UndoActionsCollection.Count == 0) return;

                ChangeRepresentationObject Undostruct = _UndoActionsCollection.Pop();
                if (Undostruct.Action == ActionType.Delete)
                {
                    AnnoListItem ali = ((AnnoTierSegment)Undostruct.UiElement).Item;
                    ((AnnoTier)Container).AnnoList.AddSorted(ali);
                    AnnoTierSegment at = ((AnnoTier)Container).AddSegment(ali);
                    this.RedoPushInUnDoForDelete(at);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                    ((AnnoTier)Container).DeleteSegment((AnnoTierSegment)Undostruct.UiElement);
                    this.RedoPushInUnDoForInsert(Undostruct.UiElement);
                }
                else if (Undostruct.Action == ActionType.Resize)
                {
                        this.RedoPushInUnDoForResize(Canvas.GetLeft(Undostruct.UiElement), Undostruct.UiElement);
                        Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                        Undostruct.UiElement.Width = Undostruct.Width;
                        ((AnnoTierSegment)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                        ((AnnoTierSegment)Undostruct.UiElement).Item.Start = Undostruct.Start;
                        ((AnnoTierSegment)Undostruct.UiElement).Item.Stop = Undostruct.Stop;
                    
                }
    
                else if (Undostruct.Action == ActionType.Move)
                {
                    this.RedoPushInUnDoForMove(Canvas.GetLeft(Undostruct.UiElement), Undostruct.UiElement);
                    Canvas.SetLeft(Undostruct.UiElement,Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    ((AnnoTierSegment)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                    ((AnnoTierSegment)Undostruct.UiElement).Item.Start = Undostruct.Start;
                    ((AnnoTierSegment)Undostruct.UiElement).Item.Stop = Undostruct.Stop;
                   
                }
                else if (Undostruct.Action == ActionType.Split)
                {
                    //resize element
                    this.RedoPushInUnDoForSplit(Canvas.GetLeft(Undostruct.UiElement), Undostruct.UiElement, Undostruct.NextUiElement);
                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;

                    ((AnnoTierSegment)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                    ((AnnoTierSegment)Undostruct.UiElement).Item.Start = Undostruct.Start;
                    ((AnnoTierSegment)Undostruct.UiElement).Item.Stop = Undostruct.Stop;

                    //delete added element
                    ((AnnoTier)Container).DeleteSegment((AnnoTierSegment)Undostruct.NextUiElement);

                }
            }

            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        public void Redo(int level)
        {
            for (int i = 1; i <= level; i++)
            {
                if (_RedoActionsCollection.Count == 0) return;

                ChangeRepresentationObject Undostruct = _RedoActionsCollection.Pop();
                if (Undostruct.Action == ActionType.Delete)
                {
                    ((AnnoTier)Container).DeleteSegment((AnnoTierSegment)Undostruct.UiElement);

                    ChangeRepresentationObject ChangeRepresentationObjectForDelete = this.MakeChangeRepresentationObjectForDelete(Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForDelete);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                     AnnoListItem ali = ((AnnoTierSegment)Undostruct.UiElement).Item;
                    ((AnnoTier)Container).AnnoList.AddSorted(ali);
                    AnnoTierSegment at = ((AnnoTier)Container).AddSegment(ali);

                    ChangeRepresentationObject ChangeRepresentationObjectForInsert = this.MakeChangeRepresentationObjectForInsert(at);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForInsert);
                }
                else if (Undostruct.Action == ActionType.Resize)
                {
                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    Undostruct.Start = ((AnnoTierSegment)Undostruct.UiElement).Item.Start;
                    Undostruct.Stop = ((AnnoTierSegment)Undostruct.UiElement).Item.Stop;
                    Undostruct.Duration = ((AnnoTierSegment)Undostruct.UiElement).Item.Duration;

                    ChangeRepresentationObject ChangeRepresentationObjectForResize = this.MakeChangeRepresentationObjectForResize(Undostruct.Margin.X, Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForResize);

                }
                else if (Undostruct.Action == ActionType.Move)
                {

                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    Undostruct.Start = ((AnnoTierSegment)Undostruct.UiElement).Item.Start;
                    Undostruct.Stop = ((AnnoTierSegment)Undostruct.UiElement).Item.Stop;
                    Undostruct.Duration = ((AnnoTierSegment)Undostruct.UiElement).Item.Duration;

                    ChangeRepresentationObject ChangeRepresentationObjectForMove = this.MakeChangeRepresentationObjectForMove(Undostruct.Margin.X, Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForMove);
                }
                else if (Undostruct.Action == ActionType.Split)
                {
                    //resize element
                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    Undostruct.Start = ((AnnoTierSegment)Undostruct.UiElement).Item.Start;
                    Undostruct.Stop = ((AnnoTierSegment)Undostruct.UiElement).Item.Stop;

                    Undostruct.Duration = Undostruct.Stop - Undostruct.Start;

                    //delete added element
                    AnnoListItem ali = ((AnnoTierSegment)Undostruct.NextUiElement).Item;
                    ((AnnoTier)Container).AnnoList.AddSorted(ali);
                    AnnoTierSegment at = ((AnnoTier)Container).AddSegment(ali);

                   
                    ChangeRepresentationObject ChangeRepresentationObjectForSplit = this.MakeChangeRepresentationObjectForSplit(Undostruct.Margin.X, Undostruct.UiElement, at);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForSplit);
                }
            }
            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        public void InsertObjectforUndoRedo(ChangeRepresentationObject dataobject)
        {
            _UndoActionsCollection.Push(dataobject);
            _RedoActionsCollection.Clear();
            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        #endregion IUndoRedo Members

        #region UndoHelperFunctions

        public ChangeRepresentationObject MakeChangeRepresentationObjectForInsert(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataObject = new ChangeRepresentationObject();
            dataObject.Action = ActionType.Insert;
            dataObject.UiElement = ApbOrDevice;
            return dataObject;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForDelete(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Delete;
            dataobject.UiElement = ApbOrDevice;
            return dataobject;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForMove(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin.X = pos;
            MoveStruct.Width = UIelement.Width;
            MoveStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            MoveStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            MoveStruct.Duration = ((AnnoTierSegment) UIelement).Item.Duration;
            MoveStruct.UiElement = UIelement;
            return MoveStruct;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForResize(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Action = ActionType.Resize;
            ResizeStruct.Margin.X = pos;
            ResizeStruct.Width = UIelement.Width;
            ResizeStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            ResizeStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            ResizeStruct.Duration = ((AnnoTierSegment)UIelement).Item.Duration;
            ResizeStruct.UiElement = UIelement;
            return ResizeStruct;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForSplit(double pos, FrameworkElement UIelement, FrameworkElement NextUIelement)
        {
            ChangeRepresentationObject SplitStruct = new ChangeRepresentationObject();
            SplitStruct.Action = ActionType.Split;
            SplitStruct.Margin.X = pos;
            SplitStruct.Width = UIelement.Width;
            SplitStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            SplitStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            SplitStruct.Duration = ((AnnoTierSegment)UIelement).Item.Duration;
            SplitStruct.UiElement = UIelement;
            SplitStruct.NextUiElement = NextUIelement;

            return SplitStruct;
        }

        public void clearUnRedo()
        {
            _UndoActionsCollection.Clear();
        }

        #endregion UndoHelperFunctions

        #region RedoHelperFunctions

        public void RedoPushInUnDoForInsert(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Insert;
            dataobject.UiElement = ApbOrDevice;
            _RedoActionsCollection.Push(dataobject);
        }

        public void RedoPushInUnDoForDelete(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Delete;
            dataobject.UiElement = ApbOrDevice;
            _RedoActionsCollection.Push(dataobject);
        }

        public void RedoPushInUnDoForMove(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin.X = pos;
            MoveStruct.Width = UIelement.Width;
            MoveStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            MoveStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            MoveStruct.Duration = ((AnnoTierSegment)UIelement).Item.Duration;
            MoveStruct.UiElement = UIelement;
            _RedoActionsCollection.Push(MoveStruct);
        }

        public void RedoPushInUnDoForResize(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Action = ActionType.Resize;
            ResizeStruct.Margin.X = pos;
            ResizeStruct.Width = UIelement.Width;
            ResizeStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            ResizeStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            ResizeStruct.Duration = ((AnnoTierSegment)UIelement).Item.Duration;
            ResizeStruct.UiElement = UIelement;
            _RedoActionsCollection.Push(ResizeStruct);
        }

        public void RedoPushInUnDoForSplit(double pos, FrameworkElement UIelement, FrameworkElement NextUiElement)
        {
            ChangeRepresentationObject SplitStruct = new ChangeRepresentationObject();
            SplitStruct.Action = ActionType.Split;
            SplitStruct.Margin.X = pos;
            SplitStruct.Width = UIelement.Width;
            SplitStruct.Start = ((AnnoTierSegment)UIelement).Item.Start;
            SplitStruct.Stop = ((AnnoTierSegment)UIelement).Item.Stop;
            SplitStruct.Duration = ((AnnoTierSegment)UIelement).Item.Duration;
            SplitStruct.UiElement = UIelement;
            SplitStruct.NextUiElement = NextUiElement;
            _RedoActionsCollection.Push(SplitStruct);
        }


        #endregion RedoHelperFunctions

        public bool IsUndoPossible()
        {
            if (_UndoActionsCollection.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsRedoPossible()
        {
            if (_RedoActionsCollection.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #endregion UndoRedo

    #region enums

    public enum ActionType
    {
        Delete = 0,
        Move = 1,
        Resize = 2,
        Insert = 3,
        Split = 4
    }

    #endregion enums

    #region datastructures

    public class ChangeRepresentationObject
    {
        public ActionType Action;
        public Point Margin;
        public double Width;
        public double Start;
        public double Stop;
        public double Duration;
        public FrameworkElement UiElement;
        public FrameworkElement NextUiElement;
    }

    #endregion datastructures
}