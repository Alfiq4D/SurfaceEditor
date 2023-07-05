using Engine;
using Engine.Models;
using Engine.Models.PredefinedModels;
using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ObjParser;
using Microsoft.Win32;
using System.IO;
using System.Globalization;
using Microsoft.Kinect;
using Vector4 = Engine.Utilities.Vector4;
using Matrix4 = Engine.Utilities.Matrix4;
using Engine.Interfaces;
using System.Reflection;

namespace Surface_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        public ObservableCollection<_3DObject> Models { get; set; } = new ObservableCollection<_3DObject>(); //models displayed on list
        public Projection Projection { get; set; }
        private WriteableBitmap bitmap;
        public Renderer Renderer { get; set; }
        Image image;
        List<ISelectable> pointsToGroup = new List<ISelectable>();
        List<Engine.Models.Point3D> pointsToBezier = new List<Engine.Models.Point3D>();
        private _3DObject displayModel;
        public _3DObject DisplayModel  //model of which details are presented
        {
            get { return displayModel; }
            set
            {
                if (displayModel == value) return;
                displayModel = value;
                NotifyPropertyChanged();
            }
        }
        ListView BezierPointsList
        {
            get
            {
                ListView listView = FindVisualChild<ListView>(DescriptionControl);
                return listView;
            }
        }

        _3DObject SelectedModel;  //model to do operation on it
        List<ICamera> Cameras = new List<ICamera>();
        ICamera activeCamera;
        Point mouseOldPosition;
        Point selectStart; //to multiselect
        Point selectEnd; //to multiselect
        bool stereographicEnabled = false;
        public Cursor3D Cursor3D { get; set; }
        private bool isInRenderingMode = true; //if false dont render on all updates
        Vector4 zVector = new Vector4(0, 0, 1, 0); //directions
        Vector4 yVector = new Vector4(0, 1, 0, 0);
        Vector4 xVector = new Vector4(1, 0, 0, 0);
        private bool isSaved = true;
        public bool IsSaved
        {
            get { return isSaved; }
            set
            {
                if (isSaved == value) return;
                isSaved = value;
                NotifyPropertyChanged();
            }
        }
        private string filenameToSave = null;
        private bool isFullScreen = false;
        bool symmetryEnabled = false;
        KinectScanner kinectScanner;
        List<string> logs = new List<string>();
        bool logsVisible = false;
        bool helpEnabled = false;
        PathGenerator pathGenerator;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            bitmap = new WriteableBitmap(screenWidth, screenHeight, 96, 96, PixelFormats.Bgr32, null);
            Renderer = new Renderer(bitmap);
            Renderer.PropertyChanged += Model_PropertyChanged;
            image = new Image();
            MainCanvas.Children.Add(image);
            image.Source = bitmap;
            Projection = new Projection(1f, 500, 80, 1920f / 1080f);
            Projection.PropertyChanged += Model_PropertyChanged;
            Cursor3D = new Cursor3D(Vector4.Zero());
            Cursor3D.PropertyChanged += Model_PropertyChanged;
            MainCanvas.Focusable = true;
            CreateCameras();
            ConfigureKinect();
            pathGenerator = new PathGenerator();
            ProcessInit();
        }

        private void ProcessInit()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            SymmetryCheckBox_Checked(null, null);
            SymmetryCheckBox.IsChecked = true;
        }

        private void ScaleScene(float currentXMin, float currentXMax, float currentYMin, float currentYMax, float targetXMin, float targetXMax, float targetYMin, float targetYMax)
        {
            foreach(var model in Models)
            {
                if(model is CylindricalBezierPatchC2)
                {
                    var surface = model as CylindricalBezierPatchC2;
                    foreach(var p in surface.ControlPoints)
                    {
                        p.Position.X = (p.Position.X - currentXMin) / (currentXMax - currentXMin) * (targetXMax - targetXMin) + targetXMin;
                        p.Position.Y = (p.Position.Y - currentYMin) / (currentYMax - currentYMin) * (targetYMax - targetYMin) + targetYMin;
                    }
                }
            }
        }

        private void TransformScene(float y)
        {
            foreach (var model in Models)
            {
                if (model is CylindricalBezierPatchC2)
                {
                    var surface = model as CylindricalBezierPatchC2;
                    foreach (var p in surface.ControlPoints)
                    {
                        p.Position.Y += y;
                    }
                }
            }
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            LogWindow  l = new LogWindow(logs);
            l.ShowDialog();
        }

        private void SetMessageLog(string text)
        {
            MessageLog.Text = text;
            logsVisible = true;
            logs.Add(text);
        }

        private void ConfigureKinect()
        {
            kinectScanner = new KinectScanner();
            if (!kinectScanner.KinectConnected)
            {
                SetMessageLog("No Kinect detected");
            }
            else
            {
                SetMessageLog("Kinect connected");
            }
        }

        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            var frame = e.OpenDepthImageFrame();

            KinektView.Source = kinectScanner.CreateBitMapFromDepthFrame(frame);
        }

        private void CreateCameras()
        {
            ICamera camera1 = new Engine.Utilities.Camera(new Vector4(0f, 0f, 10f), 1, 1);
            ICamera camera2 = new PointCamera(new Vector4(0f, 0f, 10f), new Vector4(0f, 0f, 0f));
            Cameras.Add(camera1);
            Cameras.Add(camera2);
            activeCamera = camera2;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Projection.A = (float)(MainCanvas.ActualWidth / MainCanvas.ActualHeight);
            activeCamera.SetCameraTarget(new Vector4(0, 0, 0, 1));
            CreateGrid();
            Update();
        }

        private void CreateGrid()
        {
            Engine.Models.PredefinedModels.Grid grid = new Engine.Models.PredefinedModels.Grid(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 50, 50, 20, 20);
            grid.PropertyChanged += Model_PropertyChanged;
            Models.Add(grid.Create());
        }


        private void CreateGridButton_Click(object sender, RoutedEventArgs e)
        {
            CreateGrid();
        }

        private void CreateModelButton_Click(object sender, RoutedEventArgs e)
        {
            //Torus torus = new Torus(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 3f, 50, 70);
            //torus.PropertyChanged += Model_PropertyChanged;
            //var t = torus.Create();
            //Models.Add(t);
            //SetSeclectedItem(torus);
            //Update();

            _3DModel model;
            SelectModelWindow window = new SelectModelWindow();
            if (window.ShowDialog() == true)
            {
                switch(window.Type)
                {
                    case SelectModelWindow.SelectedModelType.Torus:
                        model = new Torus(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 3f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.AstroidalEllipsoid:
                        model = new AstroidalEllipsoid(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 10f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.BohemianDome:
                        model = new BohemianDome(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 4f, 3f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Coil:
                        model = new Coil(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 4f, 3f, 2f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Cornucopia:
                        model = new Cornucopia(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 1f / 4, 1f / 4, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Cross_Cap:
                        model = new Cross_Cap(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 3f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Elasticity:
                        model = new Elasticity(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 5f, 1f, 3f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Ellipsoid:
                        model = new Ellipsoid(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 4f, 2f, 1f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Figure8:
                        model = new Figure8(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 1f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Horn:
                        model = new Horn(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 1f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.KlainBottle:
                        model = new KlainBottle(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 3f, 4f, 2f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.MorinsSurface:
                        model = new MorinsSurface(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 1f, 6f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Pear:
                        model = new Pear(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 6f, 2f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Sea_Shell:
                        model = new Sea_Shell(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 2f, 2f, 6f, -1.0f / 20, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.SineTorus:
                        model = new SineTorus(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 3f, 4f, 50, 70);
                        break;
                    case SelectModelWindow.SelectedModelType.Sphere:
                        model = new Sphere(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 50, 70);
                        break;
                    default:
                        model = new Torus(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1), 8f, 3f, 50, 70);
                        break;
                }
                model.PropertyChanged += Model_PropertyChanged;
                var m = model.Create();
                Models.Add(model);
                SetSeclectedItem(model);
                Update();
            }
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void LookAtButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel != null)
            {
                activeCamera.SetCameraTarget(SelectedModel.Position);
                SetMessageLog("Camera looking at " + SelectedModel.Name);
                Update(false);
            }
        }

        private void Update(bool changed  = true)
        {
            if (isInRenderingMode)
            {
                // bitmap.Lock();
                using (bitmap.GetBitmapContext())
                {
                    if (stereographicEnabled)
                        Renderer.RenderStereographic(Models, Cursor3D, activeCamera, Projection, (int)MainCanvas.ActualWidth, (int)MainCanvas.ActualHeight);
                    else
                        Renderer.Render(Models, Cursor3D, activeCamera, Projection, (int)MainCanvas.ActualWidth, (int)MainCanvas.ActualHeight, OrthographicCheckBox.IsChecked == true ? true : false);
                    // bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                }
                // bitmap.Unlock();
                MainCanvas.Focus();
                if (changed)
                {
                    if (!logsVisible)
                        MessageLog.Text = null;
                    logsVisible = false;
                    IsSaved = false;
                }
            }
        }

        private void PauseRender()
        {
            isInRenderingMode = false;
        }

        private void StartRender(bool changed = true)
        {
            isInRenderingMode = true;
            Update(changed);
        }

        private void ObjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SelectedModel = (_3DObject)e.AddedItems[0];
                CheckForIntersectionCurveInSelection(SelectedModel);
            }
            //Visibility of button to create bezier from points set
            bool onlyPoints = true;
            if (ObjectsList.SelectedItems == null || ObjectsList.SelectedItems.Count <= 0)
                onlyPoints = false;
            foreach (_3DObject o in ObjectsList.SelectedItems)
            {
                if (!(o is Engine.Models.Point3D p))
                    onlyPoints = false;
            }
            if (SelectedModel == null)
                onlyPoints = false;
            if (ObjectsList.SelectedItems.Count < 2)
                onlyPoints = false;
            if (onlyPoints)
                pointsToBezier = ObjectsList.SelectedItems.Cast<Engine.Models.Point3D>().ToList();
            else
                pointsToBezier.Clear();
            Update();
        }

        private void UpdateSelectedPoints(List<ISelectable> points)
        {
            foreach (ISelectable p in pointsToGroup)
                p.IsSelectedToGroup = false;
            pointsToGroup = points;
            foreach (ISelectable p in pointsToGroup)
                p.IsSelectedToGroup = true;
        }

        private void ClearSelectedPoints()
        {
            foreach (ISelectable p in pointsToGroup)
                p.IsSelectedToGroup = false;
            pointsToGroup.Clear();
        }

        private void PointsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count > 0)
            //    ChangeSelectedModel((_3DObject)e.AddedItems[0]);
            //else
            //    ChangeSelectedModel(null);
        }

        private void ChangeSelectedModel(_3DObject model)
        {
            if (model == null)
            {
                ObjectsList.SelectedItems.Clear();
                if (BezierPointsList != null)
                    if (BezierPointsList.SelectedItems.Count > 0)
                        BezierPointsList.SelectedItems.Clear();
                SelectedModel = null;
            }
            else
            {
                if (SelectedModel != null)
                {
                    SelectedModel = null;
                }
                if (model != null)
                {
                    SelectedModel = model;
                }
            }
            CheckForIntersectionCurveInSelection(SelectedModel);
            Update();
        }

        private void DeletePointFromBezierCurve(Curve bezier, Engine.Models.Point3D p)
        {
            bezier.DeletePoint(p);
            if (bezier.ControlPoints.Count == 0)
            {
                Models.Remove(bezier);
                SetSeclectedItem(Models[Models.Count - 1]);
            }
            else
            {
                SelectedModel = bezier.ControlPoints.Count - 1 >= 0 ? bezier.ControlPoints.ElementAt(bezier.ControlPoints.Count - 1) : null;
                CheckForIntersectionCurveInSelection(SelectedModel);
                if (BezierPointsList != null)
                {
                    BezierPointsList.SelectedItem = SelectedModel;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            //Delete point of bezier curve
            if (SelectedModel is Engine.Models.Point3D p)
            {
                Curve bezier = CurvesContainPoint(p);
                if (bezier != null)
                {
                    //Delete point of bezier curve
                    DeletePointFromBezierCurve(bezier, p);
                    SetMessageLog("Deleted objects: (1)");
                    return;
                }
                else if (Models.Contains(p))
                {
                    DeleteAllSelectedObjects();
                }          
            }
            //Delete all selected scene objects
            else
            {
                DeleteAllSelectedObjects();
            }
            Update();
        }

        private void DeleteAllSelectedObjects()
        {
            List<_3DObject> toDel = new List<_3DObject>();
            if (ObjectsList.SelectedItems.Contains(SelectedModel))
            {
                foreach (_3DObject obj in ObjectsList.SelectedItems)
                    toDel.Add(obj);
                foreach (_3DObject obj in toDel)
                {
                    if (obj != null)
                    {
                        if (obj is Curve b)
                        {
                            foreach (Engine.Models.Point3D point in b.ControlPoints)
                            {
                                Models.Add(point);
                            }
                        }
                        if(obj is IntersectionCurve)
                        {
                            CancelTrimming1_Click(null, null);
                            CancelTrimming2_Click(null, null);
                        }
                        if (Models.Contains(obj))
                        {
                            Models.Remove(obj);
                            SetSeclectedItem(Models.Count - 1 >= 0 ? Models.ElementAt(Models.Count - 1) : null);
                        }
                    }
                }
                SetMessageLog("Deleted objects: (" + toDel.Count.ToString() + ")");
            }
        }

        private Curve CurvesContainPoint(Engine.Models.Point3D point)
        {
            foreach (_3DObject obj in Models)
            {
                if (obj is Curve curve)
                {
                    if (curve.ControlPoints.Contains(point))
                    {
                        return curve;
                    }
                }
            }
            return null;
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            activeCamera.Zoom(e.Delta > 0 ? -1f : 1f);
            Update(false);
        }

        private void OrthographicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void OrthographicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);
            //Camera zoom and rotation
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                PauseRender();
                if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        activeCamera.Zoom((float)(mouseOldPosition.Y - pos.Y) > 0 ? -0.8f : 0.8f);
                    }
                    else
                    {
                        activeCamera.Move(-(float)(mouseOldPosition.X - pos.X) / 700f, (float)(mouseOldPosition.Y - pos.Y) / 700f);
                    }
                    mouseOldPosition = pos;
                }
                StartRender(false);
            }
            //Cursor zoom and position
            if (e.RightButton == MouseButtonState.Pressed)
            {
                //PauseRender(); //functions itself has update control
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    float zoomFactor = 0.05f;
                    if ((mouseOldPosition.Y - pos.Y) > 0)
                    {
                        MoveCursorWorld((Cursor3D.Position.X - activeCamera.CameraPosition.X) * zoomFactor, (Cursor3D.Position.Y - activeCamera.CameraPosition.Y) * zoomFactor, (Cursor3D.Position.Z - activeCamera.CameraPosition.Z) * zoomFactor);
                    }
                    else
                    {
                        MoveCursorWorld((-Cursor3D.Position.X + activeCamera.CameraPosition.X) * zoomFactor, (-Cursor3D.Position.Y + activeCamera.CameraPosition.Y) * zoomFactor, (-Cursor3D.Position.Z + activeCamera.CameraPosition.Z) * zoomFactor);
                    }
                    mouseOldPosition = pos;
                }
                else
                {
                    SetCurorPosition((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
                    ChangeVoxels(pos);
                }
                //StartRender(false);
            }
        }

        private void ChangeVoxels(Point pos)
        {
            if (Cursor3D.mode == Cursor3D.Mode.addVoxels)
            {
                Voxel v = SpaceManager.FindClosestVoxel(pos, Models, false);
                if (v != null)
                    v.isActive = true;
            }
            if (Cursor3D.mode == Cursor3D.Mode.removeVoxels)
            {
                Voxel v = SpaceManager.FindClosestVoxel(pos, Models, true);
                if (v != null)
                    v.isActive = false;
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PauseRender();
            //Grouping points
            mouseOldPosition = e.GetPosition(this);
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                var obj = SelectPointToGroup(e.GetPosition(this));
                if (obj != null)
                {
                    if (obj is ISelectable p)
                    {
                        if (pointsToGroup.Contains(p))
                        {
                            pointsToGroup.Remove(p);
                            p.IsSelectedToGroup = false;
                        }
                        else
                        {
                            pointsToGroup.Add(p);
                            p.IsSelectedToGroup = true;
                        }
                    }
                }
                selectStart = e.GetPosition(this);
            }
            else
            {
                //on left only select point
                TrySelectPoint(e.GetPosition(this));
                if (SelectedModel != Cursor3D.selectedObject)
                    Cursor3D.selectedObject = null;
            }
            StartRender(false);
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                selectEnd = e.GetPosition(this);
                List<_3DObject> points = SpaceManager.SelectAllPointsInRect(selectStart, selectEnd, Models);
                foreach (ISelectable p in points)
                {
                    if (!pointsToGroup.Contains(p))
                    {
                        pointsToGroup.Add(p);
                        p.IsSelectedToGroup = true;
                    }
                }
                if(points.Count > 0)
                    SetMessageLog("Selected " + points.Count.ToString() + " points");
                Update();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.LeftCtrl)
            {
                PauseRender();
                float modelShift = 0.04f;
                float shiftDistance = 1.0f;
                if (e.Key == Key.T)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.X += shiftDistance;
                    Update();
                }
                else if (e.Key == Key.G)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.X -= shiftDistance;
                    Update();
                }
                if (e.Key == Key.Y)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.Z += shiftDistance;
                    Update();
                }
                else if (e.Key == Key.H)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.Z -= shiftDistance;
                    Update();
                }
                if (e.Key == Key.U)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.Y += shiftDistance;
                    Update();
                }
                else if (e.Key == Key.J)
                {
                    if (SelectedModel != null)
                        SelectedModel.Position.Y -= shiftDistance;
                    Update();
                }
                if (e.Key == Key.A)
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.X += modelShift;
                    Update();
                }
                else if (e.Key == Key.D)
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.X -= modelShift;
                    Update();
                }
                if (e.Key == Key.W)
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.Z += modelShift;
                    Update();
                }
                else if (e.Key == Key.S && !Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.Z -= modelShift;
                    Update();
                }
                if (e.Key == Key.Q)
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.Y += modelShift;
                    Update();
                }
                else if (e.Key == Key.E)
                {
                    if (SelectedModel != null)
                        SelectedModel.Rotation.Y -= modelShift;
                    Update();
                }
                if (e.Key == Key.Left)
                {
                    activeCamera.MovePosition(-0.4f, 0);
                    Update(false);
                }
                else if (e.Key == Key.Right)
                {
                    activeCamera.MovePosition(0.4f, 0);
                    Update(false);
                }
                if (e.Key == Key.Up)
                {
                    activeCamera.MovePosition(0, 0.4f);
                    Update(false);
                }
                else if (e.Key == Key.Down)
                {
                    activeCamera.MovePosition(0f, -0.4f);
                    Update(false);
                }
                if (e.Key == Key.NumPad8)
                {
                    MoveCursorWorld(0, 0.5f, 0);
                    Update();
                }
                else if (e.Key == Key.NumPad2)
                {
                    MoveCursorWorld(0, -0.5f, 0);
                    Update();
                }
                if (e.Key == Key.NumPad4)
                {
                    MoveCursorWorld(-0.5f, 0, 0);
                    Update();
                }
                else if (e.Key == Key.NumPad6)
                {
                    MoveCursorWorld(0.5f, 0, 0);
                    Update();
                }
                if (e.Key == Key.NumPad7)
                {
                    MoveCursorWorld(0, 0, -0.5f);
                    Update();
                }
                else if (e.Key == Key.NumPad9)
                {
                    MoveCursorWorld(0, 0, 0.5f);
                    Update();
                }
                if (e.Key == Key.Space)
                {
                    TryCatchPoint();
                    Update();
                }
                if (e.Key == Key.Delete)
                {
                    DeleteButton_Click(null, null);
                    Update();
                }
                if(e.Key == Key.V)
                {
                    if (SelectedModel != null)
                        SelectedModel.IsVisible = !SelectedModel.IsVisible;
                }
                if(e.Key == Key.Escape)
                {
                    if (isFullScreen)
                        ToggleFullScreen();
                    if (helpEnabled)
                        ToggleHelpPanel();
                }
                if (e.Key == Key.C)
                {
                    ChangeCamera();
                }
                if ((MainCanvas.IsFocused || ObjectsList.IsFocused) && e.Key == Key.Tab && !Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (Models.Count > 0)
                    {
                        if (ObjectsList.SelectedIndex == -1)
                            SetSeclectedItem(Models[0]);
                        else
                            SetSeclectedItem(Models[(ObjectsList.SelectedIndex + 1) % Models.Count]);
                    }
                    e.Handled = true;
                }
                if ((MainCanvas.IsFocused || ObjectsList.IsFocused) && e.Key == Key.Tab && Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (Models.Count > 0)
                    {
                        if (ObjectsList.SelectedIndex == -1)
                            SetSeclectedItem(Models[0]);
                        else
                            SetSeclectedItem(Models[(ObjectsList.SelectedIndex - 1 + Models.Count) % Models.Count]);
                    }
                    e.Handled = true;
                }
                if (e.Key == Key.F1)
                    ToggleHelpPanel();
                if (e.Key == Key.F)
                    ToggleFullScreen();
                if (e.Key == Key.P)
                    CreatePointButton_Click(null, null);
                StartRender();
                if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
                    SaveScene(filenameToSave);
            }
        }

        private void ChangeCamera_Click(object sender, RoutedEventArgs e)
        {
            ChangeCamera();
        }

        private void ChangeCamera()
        {
            int index = Cameras.IndexOf(activeCamera);
            activeCamera = Cameras[(index + 1) % (Cameras.Count)];
            SetMessageLog("Camera changed");
            Update(false);
        }

        private void ToggleHelpPanel()
        {
            if (helpEnabled)
            {
                HelpPanel.Visibility = Visibility.Collapsed;
            }
            else if (!helpEnabled)
            {
                HelpPanel.Visibility = Visibility.Visible;
                HelpPanel.Focus();
            }
            helpEnabled = !helpEnabled;
        }

        private void ToggleFullScreen()
        {
            if (isFullScreen)
            {
                UIGrid.Visibility = Visibility.Visible;
                WindowStyle = WindowStyle.ToolWindow;
            }
            else
            {
                UIGrid.Visibility = Visibility.Collapsed;
                WindowStyle = WindowStyle.None;
            }
            isFullScreen = !isFullScreen;

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Projection.A = (float)(MainCanvas.ActualWidth / MainCanvas.ActualHeight);
            Update();
        }

        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainCanvas.Focus();
            //on middle button select point and place cursor on that posiotion
            if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Pressed)
            {
                PauseRender();
                // SetCurorPosition((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
                //TryCatchPoint();
                TrySelectPoint(e.GetPosition(this));
                if (SelectedModel != null)
                {
                    Cursor3D.Position.X = SelectedModel.Position.X;
                    Cursor3D.Position.Y = SelectedModel.Position.Y;
                    Cursor3D.Position.Z = SelectedModel.Position.Z;
                    if (Cursor3D.selectedObject == SelectedModel)
                        Cursor3D.selectedObject = null;
                    else
                        Cursor3D.selectedObject = SelectedModel;
                }
                else
                    Cursor3D.selectedObject = null;
                StartRender(false);
            }
        }

        private void StereoscopicButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            stereographicEnabled = !stereographicEnabled;
            if (stereographicEnabled)
            {
                SetMessageLog("Stereoscopic view enabled");
                EyeDistanceDock.Visibility = Visibility.Visible;
                button.Background = new SolidColorBrush(Colors.LightSlateGray);
            }
            else
            {
                SetMessageLog("Stereoscopic view disabled");
                EyeDistanceDock.Visibility = Visibility.Collapsed;
                button.ClearValue(BackgroundProperty);
            }
            Update();
        }

        private void CreatePointButton_Click(object sender, RoutedEventArgs e)
        {
            Engine.Models.Point3D point = new Engine.Models.Point3D(new Vector4(Cursor3D.Position.X, Cursor3D.Position.Y, Cursor3D.Position.Z));
            point.PropertyChanged += Model_PropertyChanged;
            if (SelectedModel is Curve b)
            {
                b.AddPoint(point);
            }
            else
            {
                Models.Add(point);
                SetSeclectedItem(point);
            }
            Update();
        }

        // select and catch point by 3D cursor
        private void TryCatchPoint()
        {
            if (Cursor3D.selectedObject != null)
            {
                Cursor3D.selectedObject = null;
                return;
            }
            _3DObject closest = null;
            closest = SpaceManager.SelectColosestObjectBy3DPosition(Cursor3D.Position, Models);
            if (closest != null)
            {
                Cursor3D.selectedObject = closest;
                SetSeclectedItem(closest);
            }
        }

        //select and unselect point by mouse
        private void TrySelectPoint(Point mousePos)
        {
            _3DObject closest = null;
            closest = SpaceManager.SelectColosestObjectByScreenPosition(mousePos, Models);
            if (closest != null)
            {
                SetSeclectedItem(closest);
            }
            else
            {
                SetSeclectedItem(null);
                ClearSelectedPoints();
            }
        }

        // select point by mouse right click, dont unselect
        private bool TryChoosePoint(Point mousePos)
        {
            _3DObject closest = null;
            closest = SpaceManager.SelectColosestObjectByScreenPosition(mousePos, Models);
            if (closest != null)
            {
                SetSeclectedItem(closest);
                return true;
            }
            return false;
        }

        private _3DObject SelectPointToGroup(Point mousePos)
        {
            return SpaceManager.SelectColosestObjectByScreenPosition(mousePos, Models);
        }     

        private void SetSeclectedItem(_3DObject o)
        {
            if (SelectedModel != null)
                SelectedModel.isSelected = false;
            if (o == null)
                ChangeSelectedModel(null);          
            if(DisplayModel != null && DisplayModel.DisplayPoints != null && BezierPointsList != null)
            {
                if (DisplayModel.DisplayPoints.Contains(o))
                {
                    BezierPointsList.SelectedItem = o;
                }
                else
                    BezierPointsList.SelectedItem = null;
            }
            if (Models.Contains(o))
            {
                if (ObjectsList.SelectedItem == o)
                    ObjectsList.SelectedItem = null;
                ObjectsList.SelectedItem = o;
                DisplayModel = o;
            }
            else if(o != null)
            {
                if(SelectedModel != null)
                    SelectedModel.isSelected = false;
                ChangeSelectedModel(o);
                o.isSelected = true;
            }
            Update();
        }

        private void SetCurorPosition(float x, float y)
        {
            Vector4 pos = Matrix4.Invert(activeCamera.ViewMatrix()) * Matrix4.Invert(Projection.PerspectiveProjectionMatrix()) * new Vector4(x * 2 / (float)MainCanvas.ActualWidth - 1, ((float)MainCanvas.ActualHeight - y) * 2 / (float)MainCanvas.ActualHeight - 1, Cursor3D.ScreenPosition.Z);
            pos = pos / pos.W;
            MoveCursorWorld(pos.X - Cursor3D.Position.X, pos.Y - Cursor3D.Position.Y, pos.Z - Cursor3D.Position.Z);
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            PauseRender();
            //TrySelectPoint(e.GetPosition(this));
            if (TryChoosePoint(e.GetPosition(this)))
            {
                Cursor3D.Position.X = SelectedModel.Position.X;
                Cursor3D.Position.Y = SelectedModel.Position.Y;
                Cursor3D.Position.Z = SelectedModel.Position.Z;
                if (Cursor3D.selectedObject == SelectedModel)
                    Cursor3D.selectedObject = null;
                else
                {
                    Cursor3D.selectedObject = null;
                    //SetCurorPosition((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
                    //Cursor3D.Position.X = SelectedModel.Position.X;
                    //Cursor3D.Position.Y = SelectedModel.Position.Y;
                    //Cursor3D.Position.Z = SelectedModel.Position.Z;
                    Cursor3D.selectedObject = SelectedModel;
                }
                StartRender(false);
            }
            else
            {
                Cursor3D.selectedObject = null;
                SetCurorPosition((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
            }
            //SetCurorPosition((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
        }

        private void MoveCursorWorld(float x, float y, float z)
        {
            isInRenderingMode = false;
            bool sceneChanged = Cursor3D.MoveCursorWorld(x, y, z, symmetryEnabled, pointsToGroup);
            isInRenderingMode = true;
            Update(sceneChanged);
        }

        private void LookAtCursorButton_Click(object sender, RoutedEventArgs e)
        {
            activeCamera.SetCameraTarget(Cursor3D.Position);
            SetMessageLog("Camera looking at Cursor3D");
            Update(false);
        }

        private void MoveCursorButton_Click(object sender, RoutedEventArgs e)
        {
            PauseRender();
            if (SelectedModel != null)
            {
                Cursor3D.selectedObject = null;
                Cursor3D.Position.X = SelectedModel.Position.X;
                Cursor3D.Position.Y = SelectedModel.Position.Y;
                Cursor3D.Position.Z = SelectedModel.Position.Z;
            }
            MainCanvas.Focus();
            SetMessageLog("Cursor3D is on the position of " + SelectedModel.Name);
            StartRender(); ;
        }

        private void InitCurve(Curve curve)
        {
            bool areFree = true;
            foreach (_3DObject o in pointsToGroup)
                if (!Models.Contains(o))
                    areFree = false;
            if (pointsToGroup.Count > 0 && areFree)
            {
                foreach (Engine.Models.Point3D p in pointsToGroup)
                {
                    Models.Remove(p);
                    p.IsSelectedToGroup = false;
                    curve.AddPoint(p);
                }
                ClearSelectedPoints();
                ObjectsList.SelectedItem = curve;
                SetSeclectedItem(curve);
            }
            else if (pointsToBezier.Count > 0)
            {
                foreach (Engine.Models.Point3D p in pointsToBezier)
                {
                    Models.Remove(p);
                    curve.AddPoint(p);
                }
                pointsToBezier.Clear();
                ObjectsList.SelectedItem = curve;
                SetSeclectedItem(curve);
            }
            else
            {
                ObjectsList.SelectedItem = curve;
                SetSeclectedItem(curve);
                CreatePointButton_Click(null, null);
            }
            Update();
            MainCanvas.Focus();
        }

        private void CreateBezier_Click(object sender, RoutedEventArgs e)
        {
            BezierCurve bezier = CreateBezierCurve();
            InitCurve(bezier);
        }

        private void ObjectsList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ObjectsList.SelectedItem != null)
            {
                DisplayModel = (_3DObject)ObjectsList.SelectedItem;
                ChangeSelectedModel((_3DObject)ObjectsList.SelectedItem);
            }
        }

        private BezierCurve CreateBezierCurve()
        {
            BezierCurve bezier = new BezierCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            bezier.PropertyChanged += Model_PropertyChanged;
            Models.Add(bezier);
            return bezier;
        }

        private void ObjectsList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ObjectsList.SelectedItem = null;
            DisplayModel = null;
        }

        private void CreateBezierC2_Click(object sender, RoutedEventArgs e)
        {
            BezierCurveC2 bezier = CreateBezierCurveC2();
            InitCurve(bezier);
        }

        private BezierCurveC2 CreateBezierCurveC2()
        {
            BezierCurveC2 bezier = new BezierCurveC2(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            bezier.PropertyChanged += Model_PropertyChanged;
            Models.Add(bezier);
            return bezier;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            BezierCurveC2 b = (BezierCurveC2)DisplayModel;
            b.ChangeRepresentationToBSpline();
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            BezierCurveC2 b = (BezierCurveC2)DisplayModel;
            b.ChangeRepresentationToBernstain();
        }

        private void YButton_Click(object sender, RoutedEventArgs e)
        {
            activeCamera.SetCameraTarget(activeCamera.CameraPosition + yVector);
            yVector.Y = -yVector.Y;
            Update(false);
        }

        private void ZButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeCamera is Engine.Utilities.Camera)
            {
                activeCamera.SetCameraTarget(activeCamera.CameraPosition + zVector);
                zVector.Z = -zVector.Z;
                Update(false);
            }
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeCamera is Engine.Utilities.Camera)
            {
                activeCamera.SetCameraTarget(activeCamera.CameraPosition + xVector);
                xVector.X = -xVector.X;
                Update(false);
            }
        }

        private void LoadObj_Click(object sender, RoutedEventArgs e)
        {
            var obj = new Obj();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Obj files (*.obj)|*.obj|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                obj.LoadObj(openFileDialog.FileName);
                // StaticObjModel model = new StaticObjModel(obj);
                MeshObjModel model = new MeshObjModel(obj);
                model.PropertyChanged += Model_PropertyChanged;
                Models.Add(model);
            }
        }

        private void SaveObj_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel is IObjectable ob)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Obj files (*.obj)|*.obj"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    ob.SaveToObj(saveFileDialog.FileName);
                }                
            }
            else
                MessageBox.Show("You have to choose any .obj compatible object.");
        }

        private InterpolationCurve CreateInterpolationCurve()
        {
            InterpolationCurve curve = new InterpolationCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            curve.PropertyChanged += Model_PropertyChanged;
            Models.Add(curve);
            return curve;
        }

        private void InterpolationCurveC2_Click(object sender, RoutedEventArgs e)
        {
            InterpolationCurve curve = CreateInterpolationCurve();
            InitCurve(curve);
        }

        private void ExactMode_Checked(object sender, RoutedEventArgs e)
        {
            if (Renderer != null)
            {
                Renderer.mode = Renderer.Mode.Exact;
                Update();
            }
        }

        private void ApproximateMode_Checked(object sender, RoutedEventArgs e)
        {
            if (Renderer != null)
            {
                Renderer.mode = Renderer.Mode.Approximate;
                Update();
            }
        }

        private void EquidistantKnots_Checked(object sender, RoutedEventArgs e)
        {
            InterpolationCurve curve = (InterpolationCurve)DisplayModel;
            curve.UseChordParam = false;
            Update();
        }

        private void ChordKnots_Checked(object sender, RoutedEventArgs e)
        {
            InterpolationCurve curve = (InterpolationCurve)DisplayModel;
            curve.UseChordParam = true;
            Update();
        }

        private void CreateBezierPatch_Click(object sender, RoutedEventArgs e)
        {
            PatchWindow window = new PatchWindow();
            if (window.ShowDialog() == true)
            {
                if (window.Type == PatchWindow.PatchType.Rectangle)
                {
                    CreateRectangleBezierPatch(window);
                }
                else if (window.Type == PatchWindow.PatchType.Cyllinder)
                {
                    CreateCylindricalBezierPatch(window);
                }
                Update();
            }
        }

        private void CreateRectangleBezierPatch(PatchWindow window)
        {
            RectangularBezierPatch bezierPatch = new RectangularBezierPatch(new Vector4(0, 0, 0, 1), window.U, window.V, window.RectangleWidth, window.RectangleHeight);
            Models.Add(bezierPatch);
            bezierPatch.PropertyChanged += Model_PropertyChanged;
            SetSeclectedItem(bezierPatch);
        }

        private void CreateCylindricalBezierPatch(PatchWindow window)
        {         
            CylindricalBezierPatch bezierPatch = new CylindricalBezierPatch(new Vector4(0, 0, 0, 1), window.U, window.V, window.CylinderRadius, window.CylinderHeight);
            Models.Add(bezierPatch);
            bezierPatch.PropertyChanged += Model_PropertyChanged;
            SetSeclectedItem(bezierPatch);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveScene();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MG files (*.mg1)|*.mg1|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                LoadScene(openFileDialog.FileName);
            }
        }

        private void LoadScene(string fileName)
        {
            try
            {
                var lines = File.ReadAllLines(fileName);
                Loader loader = new Loader();
                loader.Load(lines);
                if (loader.Errors > 0)
                {
                    MessageBox.Show($"Could not load {loader.Errors} scene objects", "Objects not loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                SetMessageLog((loader.Loaded - loader.Errors).ToString() + " scene objects loaded");
                foreach (var model in loader.LoadedModels)
                {
                    model.PropertyChanged += Model_PropertyChanged;
                    Models.Add(model);
                }
                Update();
            }
            catch
            {
                MessageBox.Show("Could not load file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveScene(string filename = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            if (filename == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "MG file (*.mg1)|*.mg1|Text file (*.txt)|*.txt"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    string text = "";
                    foreach (_3DObject obj in Models)
                    {
                        text += obj.Save();
                    }
                    File.WriteAllText(saveFileDialog.FileName, text);
                    filenameToSave = saveFileDialog.FileName;
                    IsSaved = true;
                }
            }
            else
            {
                string text = "";
                foreach (_3DObject obj in Models)
                {
                    text += obj.Save();
                }
                File.WriteAllText(filename, text);
                IsSaved = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!IsSaved && Models.Count > 0 && (Models.Count > 1 || !(Models[0] is Engine.Models.PredefinedModels.Grid)))
            {
                string msg = "Actual scene is not saved. Close without saving?";
                MessageBoxResult result = MessageBox.Show(msg, "Data App", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void CreateBezierPatchC2_Click(object sender, RoutedEventArgs e)
        {
            PatchWindow window = new PatchWindow();
            if (window.ShowDialog() == true)
            {
                if (window.Type == PatchWindow.PatchType.Rectangle)
                {
                    CreateRectangleBezierPatchC2(window);
                }
                else if (window.Type == PatchWindow.PatchType.Cyllinder)
                {
                    CreateCylindricalBezierPatchC2(window);
                }
                Update();
            }
        }

        private void CreateRectangleBezierPatchC2(PatchWindow window)
        {         
            RectangularBezierPatchC2 bezierPatch = new RectangularBezierPatchC2(new Vector4(0, 0, 0, 1), window.U, window.V, window.RectangleWidth, window.RectangleHeight);
            Models.Add(bezierPatch);
            bezierPatch.PropertyChanged += Model_PropertyChanged;
            SetSeclectedItem(bezierPatch);
        }

        private void CreateCylindricalBezierPatchC2(PatchWindow window)
        {         
            CylindricalBezierPatchC2 bezierPatch = new CylindricalBezierPatchC2(new Vector4(0, 0, 0, 1), window.U, window.V, window.CylinderRadius, window.CylinderHeight);
            Models.Add(bezierPatch);
            bezierPatch.PropertyChanged += Model_PropertyChanged;
            SetSeclectedItem(bezierPatch);
        }

        private void SymmetryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            symmetryEnabled = true;
            CheckAllCylinderForSymetry();
        }

        private void SymmetryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            symmetryEnabled = false;
        }

        private void CheckAllCylinderForSymetry()
        {
            PauseRender();
            foreach (var obj in ObjectsList.SelectedItems)
            {
                if (obj is CylindricalBezierPatchC2 patch)
                {
                    patch.CorrectCymmetricalPoints();
                }
            }
            StartRender();
        }

        private void CreateMirrorButton_Click(object sender, RoutedEventArgs e)
        {
            PauseRender();
            int counter = 0;
            foreach (var obj in ObjectsList.SelectedItems)
            {
                _3DObject newObj = ((_3DObject)obj).CloneMirrored();
                AddClonedObjectToScene(newObj);
                counter++;
            }
            if(counter > 0)
                SetMessageLog(counter.ToString() + " mirrored object created");
            StartRender();
        }

        private void CreateClonedButton_Click(object sender, RoutedEventArgs e)
        {
            PauseRender();
            int counter = 0;
            foreach (var obj in ObjectsList.SelectedItems)
            {
                _3DObject newObj = ((_3DObject)obj).Clone();
                AddClonedObjectToScene(newObj);
                counter++;
            }
            if (counter > 0)
                SetMessageLog(counter.ToString() + " cloned object created");
            StartRender();
        }

        private void AddClonedObjectToScene(_3DObject obj)
        {
            Models.Add(obj);
            obj.PropertyChanged += Model_PropertyChanged;
        }

        private void PointsVisibleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Renderer != null)
            {
                Renderer.ArePointVisible = true;
                Update();
            }
        }

        private void PointsVisibleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Renderer != null)
            {
                Renderer.ArePointVisible = false;
                Update();
            }
        }

        private void CreateVoxelGrid_Click(object sender, RoutedEventArgs e)
        {
            VoxelGrid voxelGrid = new VoxelGrid(2);
            Models.Add(voxelGrid);
            voxelGrid.PropertyChanged += Model_PropertyChanged;
            SetSeclectedItem(voxelGrid);
            Update();
        }

        private void StartAnimation_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsList.SelectedItem is VoxelGrid grid)
                grid.StartAnimation();
            else
                SetMessageLog("Select VoxelGrid first");
            Update();
        }

        private void ConvertToObj_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel is VoxelGrid grid)
            {
                StaticObjModel model = grid.ConvertToObj();
                Models.Add(model);
                model.PropertyChanged += Model_PropertyChanged;
                Update();
            }
            else
                SetMessageLog("Select VoxelGrid to convert");
        }

        private void FusePoints_Click(object sender, RoutedEventArgs e)
        {
            if(pointsToGroup.Count == 2)
            {
                if(pointsToGroup[0] is Engine.Models.Point3D point1 && pointsToGroup[1] is Engine.Models.Point3D point2)
                {
                    Engine.Models.Point3D point = new Engine.Models.Point3D(new Vector4((point1.Position.X + point2.Position.X) / 2, (point1.Position.Y + point2.Position.Y) / 2, (point1.Position.Z + point2.Position.Z) / 2));
                    pointsToGroup.Remove(point1);
                    pointsToGroup.Remove(point2);
                    point1.IsSelectedToGroup = false;
                    point2.IsSelectedToGroup = false;
                    foreach (var model in Models)
                    {
                        if (model is Curve curve)
                        {
                            curve.FindAndSubstitutePointsInPatch(point1, point2, point);
                        }
                    }
                }
                else if (pointsToGroup[0] is ArtificialPoint3D fp1 && pointsToGroup[1] is ArtificialPoint3D fp2)
                {
                    ArtificialPoint3D fp = new ArtificialPoint3D(new Vector4((fp1.Position.X + fp2.Position.X) / 2, (fp1.Position.Y + fp2.Position.Y) / 2, (fp1.Position.Z + fp2.Position.Z) / 2))
                    {
                        isFused = true
                    };
                    fp.PropertyChanged += fp1.InitialPosition_PropertyChanged;
                    pointsToGroup.Remove(fp1);
                    pointsToGroup.Remove(fp2);
                    fp1.IsSelectedToGroup = false;
                    fp2.IsSelectedToGroup = false;
                    foreach (var model in Models)
                    {
                        if(model is SurfacePatch surface)
                        {
                            surface.FindAndSubstitutePointsInPatch(fp1, fp2, fp);
                        }
                    }
                }
                Update();
            }
            else
            {
                SetMessageLog("Fusing not possible, select two points first");
            }
        }

        private void CreateGregoryPatch_Click(object sender, RoutedEventArgs e)
        {
            bool error = false;
            if (CheckSelectedItemsForFillIn())
            {
                List<List<ArtificialPoint3D>> connections = new List<List<ArtificialPoint3D>>();
                List<SurfacePatch> surfacePatches = new List<SurfacePatch>();
                foreach (var patch in ObjectsList.SelectedItems)
                {
                    surfacePatches.Add((SurfacePatch)patch);                   
                    connections.Add(((SurfacePatch)patch).GetJoinedPointsFromSurface());
                }
                if(connections.All(c => c.Count >= 2))
                {
                    FilterSelectedPoints(connections);
                }
                else
                    error = true;
                if (connections.All(c => c.Count == 2) && CheckCycle(connections, surfacePatches))
                {
                    GregoryPatch gregoryPatch = new GregoryPatch(connections, surfacePatches);
                    Models.Add(gregoryPatch);
                    gregoryPatch.PropertyChanged += Model_PropertyChanged;
                    Update();
                }
                else
                    error = true;
            }
            else
                error = true;
            if (error)
                MessageBox.Show("You have to select at least three patches connected in corners", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                SetMessageLog("Gregory patch created");
        }

        private bool CheckSelectedItemsForFillIn()
        {
            if (ObjectsList.SelectedItems.Count < 3)
                return false;
            foreach (var obj in ObjectsList.SelectedItems)
            {
                if (obj is RectangularBezierPatch p1)
                {
                    if (p1.SinglePatches.Count != 1)
                        return false;
                }
                else
                    return false;
            }
            return true;
        }

        private void FilterSelectedPoints(List<List<ArtificialPoint3D>> connections)
        {
            foreach (var list in connections)
            {
                List<ArtificialPoint3D> toDel = new List<ArtificialPoint3D>();
                foreach (var point in list)
                {
                    bool found = false;
                    foreach (var otherList in connections)
                    {
                        if (otherList != list)
                            foreach (var otherPoint in otherList)
                            {
                                if (otherPoint == point)
                                    found = true;
                            }
                    }
                    if (!found)
                        toDel.Add(point);
                }
                foreach (var p in toDel)
                    list.Remove(p);
            }
            return;
        }

        private bool CheckCycle(List<List<ArtificialPoint3D>> connections, List<SurfacePatch> surfacePatches)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                var indexes1 = surfacePatches[i].GetPointIndex(connections[i][0]);
                var indexes2 = surfacePatches[i].GetPointIndex(connections[i][1]);
                if (!(indexes1.i != -1 && indexes1.j != -1 && indexes2.i != -1 && indexes2.j != -1 && (indexes1.i == indexes2.i || indexes1.j == indexes2.j)))
                    return false;
            }
            return true;
        }

        private void StartScaning_Click(object sender, RoutedEventArgs e)
        {
            if (kinectScanner != null && kinectScanner.KinectConnected && ObjectsList.SelectedItem is VoxelGrid grid)
            {
                kinectScanner.sensor.AllFramesReady += Sensor_AllFramesReady;
                kinectScanner.StartScanning(ObjectsList.SelectedItem as VoxelGrid);
                KinektView.Visibility = Visibility.Visible;
                NextScanButton.Visibility = Visibility.Visible;
                SetMessageLog("Starting scaning with Kinect");
            }
            else
            {
                SetMessageLog("Scaning not possible, Kinect not detected or VoxelGrid not selected");
            }
        }

        private void NextScanButton_Click(object sender, RoutedEventArgs e)
        {
            VoxelGrid scaningGrid = kinectScanner.NextScan();
            StaticObjModel model = scaningGrid.ConvertToObj();
            Models.Add(model);
            model.PropertyChanged += Model_PropertyChanged;
            Update();
            if (kinectScanner.Completed)
            {
                kinectScanner.sensor.AllFramesReady -= Sensor_AllFramesReady;
                KinektView.Visibility = Visibility.Collapsed;
                NextScanButton.Visibility = Visibility.Collapsed;
                SetMessageLog("Scanning ended successfully");
            }
        }

        private void SetPointsPosition_Click(object sender, RoutedEventArgs e)
        {
            SetPositionWindow window = new SetPositionWindow();
            if (window.ShowDialog() == true)
            {
                if (window.ToChange == SetPositionWindow.Source.ListView)
                {
                    var pointList = BezierPointsList;
                    if (pointList != null)
                    {
                        SpaceManager.SetPositionToPointsSet(pointList.SelectedItems.Cast<_3DObject>().ToList(), 
                            window.SetXEnable, window.SetYEnable, window.SetZEnable, symmetryEnabled, window.XVal, window.YVal, window.ZVal);
                        SetMessageLog("Position of " + pointList.SelectedItems.Count.ToString() + "points updated");
                    }
                }
                if(window.ToChange == SetPositionWindow.Source.Selection)
                {
                    SpaceManager.SetPositionToPointsSet(pointsToGroup.Cast<_3DObject>().ToList(),
                            window.SetXEnable, window.SetYEnable, window.SetZEnable, symmetryEnabled, window.XVal, window.YVal, window.ZVal);
                    SetMessageLog("Position of " + pointsToGroup.Count.ToString() +"points updated");
                }              
                Update();
            }
        }

        private void IntersectSurfaces_Click(object sender, RoutedEventArgs e)
        {
            IntersectionStep window = new IntersectionStep();
            if (window.ShowDialog() == true)
            {
                if (ObjectsList.SelectedItems.Count == 2)
                {
                    if ((ObjectsList.SelectedItems[0] is IIntersectable) && (ObjectsList.SelectedItems[1] is IIntersectable ))
                        Intersect((IIntersectable)ObjectsList.SelectedItems[0], (IIntersectable)ObjectsList.SelectedItems[1], window.Eps);
                }
                else if (ObjectsList.SelectedItems.Count == 1)
                {
                    if ((ObjectsList.SelectedItems[0] is IIntersectable))
                        Intersect((IIntersectable)ObjectsList.SelectedItems[0], (IIntersectable)ObjectsList.SelectedItems[0], window.Eps, true);
                }
                else
                {
                    SetMessageLog("Intersecting not possible, select intersection max two intersectable objects first");
                }
            }
        }

        private void Intersect(IIntersectable obj1, IIntersectable obj2, float stepEps, bool selfIntersection = false)
        {
            Intersector intersector = new Intersector(true, -0.2f);
            intersector.Intersect(obj1, obj2, stepEps, Cursor3D.Position, selfIntersection);
            IntersectionCurve curve = new IntersectionCurve(intersector.Points, obj1, obj2, intersector.Surface1Parameters, intersector.Surface2Parameters, ParametersIntersection1.ActualWidth, ParametersIntersection1.ActualHeight);
            Models.Add(curve);
            curve.PropertyChanged += Model_PropertyChanged;
            Update();
        }

        private void CheckForIntersectionCurveInSelection(_3DObject selectedModel)
        {
            if (SelectedModel is IntersectionCurve ic && SelectedModel.IsVisible)
                SelectedIntersection(ic);
            else
                UnSelectedIntersection();
        }

        private void SelectedIntersection(IntersectionCurve selectedModel)
        {        
            ParametersIntersection1.Children.Clear();
            foreach (var l in selectedModel.lines1)
                ParametersIntersection1.Children.Add(l);
            ParametersIntersection2.Children.Clear();
            foreach (var l in selectedModel.lines2)
                ParametersIntersection2.Children.Add(l);
            if(selectedModel.S1 != null)
                Obj1Name.Text = ((_3DObject)selectedModel.S1).Name;
            if (selectedModel.S2 != null)
                Obj2Name.Text = ((_3DObject)selectedModel.S2).Name;
            Parametrization1.Visibility = Visibility.Visible;
            Parametrization2.Visibility = Visibility.Visible;
        }

        private void UnSelectedIntersection()
        {
            ParametersIntersection1.Children.Clear();
            ParametersIntersection2.Children.Clear();
            Parametrization1.Visibility = Visibility.Collapsed;
            Parametrization2.Visibility = Visibility.Collapsed;
            Parametrization1.IsHitTestVisible = false;
            Parametrization2.IsHitTestVisible = false;
        }

        private void ConvertToInterpolation_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedModel is IntersectionCurve ic)
            {
                InterpolationCurve curve = ic.ConvertToInterpolationCurve();
                curve.PropertyChanged += Model_PropertyChanged;
                Models.Add(curve);
                SetMessageLog("Interpolation curve created");
                Update();
            }
            else
            {
                SetMessageLog("Converting not possible, select intersection curve first");
            }
        }

        private void TrimSurface_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel is IntersectionCurve)
            {
                Parametrization1.IsHitTestVisible = true;
                Parametrization2.IsHitTestVisible = true;
                MessageBox.Show("Click on the parametrisation above to start trimming");
            }
            else
            {
                SetMessageLog("Trimming not possible, select intersection curve first");
            }
        }

        private void ParametersIntersection1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SelectedModel is IntersectionCurve curve)
            {
                Point start = e.GetPosition(ParametersIntersection1);
                curve.UpdateFirstTrimmingTable(start.X / ParametersIntersection1.ActualWidth, start.Y / ParametersIntersection1.ActualHeight, 128);
            }
            Parametrization1.IsHitTestVisible = false;
            Parametrization2.IsHitTestVisible = false;
            Update();
            e.Handled = true;
        }

        private void ParametersIntersection2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(SelectedModel is IntersectionCurve curve)
            {
                Point start = e.GetPosition(ParametersIntersection2);
                curve.UpdateSecondTrimmingTable(start.X / ParametersIntersection2.ActualWidth, start.Y / ParametersIntersection2.ActualHeight, 128);
            }
            Parametrization1.IsHitTestVisible = false;
            Parametrization2.IsHitTestVisible = false;
            Update();
            e.Handled = true;
        }

        private void CancelTrimming1_Click(object sender, RoutedEventArgs e)
        {
            if(DisplayModel is IntersectionCurve curve)
            {
                curve.RemoveFirstTrimmingTable();
                Update();
            }
        }

        private void CancelTrimming2_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayModel is IntersectionCurve curve)
            {
                curve.RemoveSecondTrimmingTable();
                Update();
            }
        }

        private void ReverseTrimming1_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayModel is IntersectionCurve curve)
            {
                curve.ReverseFirstTrimming();
                Update();
            }
        }

        private void ReverseTrimming2_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayModel is IntersectionCurve curve)
            {
                curve.ReverseSecondTrimming();
                Update();
            }
        }

        private void DoTrimming1_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel is IntersectionCurve curve)
            {
                curve.UpdateFirstTrimmingTable(0, 0, 128);
            }
            Update();
        }

        private void DoTrimming2_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModel is IntersectionCurve curve)
            {
                curve.UpdateSecondTrimmingTable(0, 0, 128);
            }
            Update();
        }

        private void CreateButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel0.Visibility = Visibility.Visible;
        }

        private void CreateButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel0.Visibility = Visibility.Collapsed;
        }

        private void ConvertButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel13.Visibility = Visibility.Visible;
        }

        private void ConvertButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel13.Visibility = Visibility.Collapsed;
        }

        private void ShowVoxels_Click(object sender, RoutedEventArgs e)
        {
            if (Cursor3D.mode == Cursor3D.Mode.addVoxels)
            {
                Cursor3D.mode = Cursor3D.Mode.normal;
                SetMessageLog("Setting back to normal mode");
            }
            else
            {
                Cursor3D.mode = Cursor3D.Mode.addVoxels;
                SetMessageLog("Setting add voxels mode");
            }
        }

        private void RemoveVoxels_Click(object sender, RoutedEventArgs e)
        {
            if (Cursor3D.mode == Cursor3D.Mode.removeVoxels)
            {
                Cursor3D.mode = Cursor3D.Mode.normal;
                SetMessageLog("Setting back to normal mode");
            }
            else
            {
                Cursor3D.mode = Cursor3D.Mode.removeVoxels;
                SetMessageLog("Setting remove voxels mode");
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleHelpPanel();
        }

        private void DrawWithSquares_Checked(object sender, RoutedEventArgs e)
        {
            if(DisplayModel is VoxelGrid voxelGrid)
            {
                voxelGrid.renderingMode = VoxelGrid.RenderingMode.squares;
                Update();
            }
        }

        private void DrawWithQubes_Checked(object sender, RoutedEventArgs e)
        {
            if (DisplayModel is VoxelGrid voxelGrid)
            {
                voxelGrid.renderingMode = VoxelGrid.RenderingMode.qubes;
                Update();
            }
        }

        private void CreatePathButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel14.Visibility = Visibility.Visible;
        }

        private void CreatePathButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExpandableButtonPanel14.Visibility = Visibility.Collapsed;
        }

        private void CreateRoughing_Click(object sender, RoutedEventArgs e)
        {
            ParametricModel model = new ParametricModel(Models.ToList());
            pathGenerator.CreateRoughingPath(model);
            SetMessageLog("Roughing path created");
        }

        private void CreateBase_Click(object sender, RoutedEventArgs e)
        {
            ParametricModel model = new ParametricModel(Models.ToList());
            var curve = pathGenerator.CreateBaseMachiningPath(model);
            Models.Add(curve);
            curve.PropertyChanged += Model_PropertyChanged;
            Update();
            SetMessageLog("Base path created");
        }

        private void CreateFinish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParametricModel model = new ParametricModel(Models.ToList());
                var curve = pathGenerator.CreateFinishMachiningPath(model);
                Models.Add(curve);
                curve.PropertyChanged += Model_PropertyChanged;
                Update();
                SetMessageLog("Finish path created");
            }
            catch (Exception ex)
            {
                SetMessageLog(ex.Message);
            }
        }

        private void CalculateHeight_Click(object sender, RoutedEventArgs e)
        {
            ParametricModel model = new ParametricModel(Models.ToList());
            pathGenerator.CreateHeightMap(model);
            SetMessageLog("Height map created");
        }
    }
}