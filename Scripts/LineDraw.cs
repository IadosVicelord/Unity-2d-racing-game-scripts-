using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class LineDraw : MonoBehaviour
{
    #region private fields
    private bool _isFirstClick = true;
    private bool _isDrawingComplete = false;
    private bool _isDrawingWheel = false;
    private bool _isRaceStarted = false;
    private float _carMass;
    private GameObject _currentLine;
    private LineRenderer _lineRenderer;
    private PolygonCollider2D _polyCollider;
    private List<Vector2> _fingerPositions;
    private Vector3 _currentWheelPosition;
    private BuildModes _currentBuildMode;
    #endregion

    #region props
    /// <summary>
    /// "Заготовка" линии образующей тело машины
    /// </summary>
    public GameObject linePrefab;
    /// <summary>
    /// "Заготовка" колеса
    /// </summary>
    public GameObject wheelPrefab;
    /// <summary>
    /// "Заготовка" угловой точки
    /// </summary>
    public GameObject cornerDot;

    /// <summary>
    /// Камера активная при рисовании машины
    /// </summary>
    public Camera buildCamera;
    /// <summary>
    /// Камера активная при игре
    /// </summary>
    public Camera carCamera;

    /// <summary>
    /// Интерфейс в игре
    /// </summary>
    public Canvas PlayCanvas;
    /// <summary>
    /// Интерфейс при "строительстве" машины
    /// </summary>
    public Canvas BuildCanvas;

    /// <summary>
    /// Текущая машина
    /// </summary>
    private GameObject CurrentLine { get => _currentLine; set => _currentLine = value; }
    /// <summary>
    /// Текущее колесо
    /// </summary>
    private GameObject CurrentAdditionalObject;
    /// <summary>
    /// Рендерер линий
    /// </summary>
    private LineRenderer LineRenderer { get => _lineRenderer; set => _lineRenderer = value; }
    /// <summary>
    /// Коллайдер для машины
    /// </summary>
    private PolygonCollider2D PolyCollider { get => _polyCollider; set => _polyCollider = value; }
    /// <summary>
    /// Список точек-углов машины
    /// </summary>
    private List<Vector2> FingerPositions { get => _fingerPositions; set => _fingerPositions = value; }
    /// <summary>
    /// Текущее положение колеса
    /// </summary>
    private Vector3 CurrentAdditionalObjectPosition { get => _currentWheelPosition; set => _currentWheelPosition = value; }

    /// <summary>
    /// Словарь "положение - размещаемый на нем объект"
    /// </summary>
    public Dictionary<Vector2, GameObject> AdditionalObjects = new Dictionary<Vector2, GameObject>();

    /// <summary>
    /// Масса машины
    /// </summary>
    private float CarMass { get => _carMass; set => _carMass = value; }

    /// <summary>
    /// Текущий клик - первый?
    /// </summary>
    private bool IsFirstClick { get => _isFirstClick; set => _isFirstClick = value; }
    /// <summary>
    /// Рисование корпуса завершено?
    /// </summary>
    private bool IsDrawingComplete { get => _isDrawingComplete; set => _isDrawingComplete = value; }
    /// <summary>
    /// Рисование колес завершено?
    /// </summary>
    private bool IsDrawingWheel { get => _isDrawingWheel; set => _isDrawingWheel = value; }
    /// <summary>
    /// Строительство машины завершено?
    /// </summary>
    public bool IsRaceStarted
    {
        get => _isRaceStarted;
        set
        {
            if (value)
            {
                //Включение физики для машины
                CurrentLine.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                //Переключение камеры
                buildCamera.enabled = false;
                carCamera.enabled = true;
                //Переключение интерфейса
                BuildCanvas.enabled = false;
                PlayCanvas.enabled = true;
            }
            _isRaceStarted = value;
        }
    }

    public BuildModes CurrentBuildMode
    {
        get { return _currentBuildMode; }
        set { _currentBuildMode = value; }
    }

    #endregion

    /// <summary>
    /// Инициализация
    /// </summary>
    void Start()
    {
        PlayCanvas.enabled = false;
        BuildCanvas.enabled = true;

        PolyCollider = new PolygonCollider2D();
        FingerPositions = new List<Vector2>();

        buildCamera.enabled = true;
        carCamera.enabled = false;
    }

    /// <summary>
    /// Вызывается каждый кадр
    /// </summary>
    void Update()
    {
        if(Input.GetMouseButton(0) && !IsRaceStarted)
        {
            switch (CurrentBuildMode)
            {
                case BuildModes.DrawingBody:
                    {
                        DrawCarBody();
                        break;
                    }
                case BuildModes.DrawingRockets:
                    {
                        break;
                    }
                case BuildModes.DrawingWheels:
                    {
                        DrawWheel();
                        break;
                    }
                case BuildModes.DrawingWheelsSize:
                    {
                        ChangeWheelSize();
                        break;
                    }
                default: break;
            }
        }

        if(Input.GetMouseButtonUp(0) && !IsRaceStarted)
        {
            switch (CurrentBuildMode)
            {
                case BuildModes.DrawingWheelsSize:
                    {
                        EndDrawingWheel();
                        break;
                    }
                case BuildModes.DrawingRocketDirection:
                    {
                        break;
                    }
                default: break;
            }
        }
    }

    /// <summary>
    /// Вызывается через промежутки времени
    /// </summary>
    void FixedUpdate()
    {
        if (IsRaceStarted)
            CamFollowCar();
    }

    /// <summary>
    /// Рисование тела машины
    /// </summary>
    private void DrawCarBody()
    {
        if (IsFirstClick)
        {
            CreateLine();
            IsFirstClick = false;
        }
        else
        {
            Vector2 currentFingerPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            UpdateLine(currentFingerPos);
        }
    }

    /// <summary>
    /// Рисование колес
    /// </summary>
    void DrawWheel()
    {
        try
        {
            //Точка на которой нужно нарисовать объект
            Vector2 drawingPosition = FingerPositions.First(x => Vector2.Distance(x, Camera.main.ScreenToWorldPoint(Input.mousePosition)) < 1f);
        
            //Если в текущей точке нет никаких объектов
            if(!AdditionalObjects.TryGetValue(drawingPosition, out GameObject additObject))
            {
                //Инициализация объекта колеса и установка его положения и базового размера
                additObject = Instantiate(wheelPrefab, Vector3.zero, Quaternion.identity);
                additObject.transform.position = drawingPosition;
                additObject.transform.localScale += Vector3.one;

                //Получение компонента колеса и установка его свойств
                WheelJoint2D wheelJoint = additObject.GetComponent<WheelJoint2D>();
                wheelJoint.connectedBody = CurrentLine.GetComponent<Rigidbody2D>();
                wheelJoint.connectedAnchor = drawingPosition;

                //Добавление объекта в словарь
                AdditionalObjects.Add(drawingPosition, additObject);

                //Поля видимые за пределами функции создания колеса
                CurrentAdditionalObject = additObject;
                CurrentAdditionalObjectPosition = drawingPosition;

                //Переключение режима "строительства"
                CurrentBuildMode = BuildModes.DrawingWheelsSize;

                return;
            }
        }
        catch
        {
            return;
        }
    }

    /// <summary>
    /// Вызывается при удерживании клавиши, изменяет размер колеса
    /// </summary>
    void ChangeWheelSize()
    {
        float distance = Vector3.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), CurrentAdditionalObjectPosition);
        CurrentAdditionalObject.transform.localScale = Vector3.one * distance * 0.045f;
    }

    /// <summary>
    /// Вызывается при отпускании клавиши, устанавливает массу колеса и завершает измненение его размера
    /// </summary>
    void EndDrawingWheel()
    {
        CurrentAdditionalObject.GetComponent<Rigidbody2D>().mass = CurrentAdditionalObject.transform.localScale.x * 0.09f;
        CurrentBuildMode = BuildModes.DrawingWheels;
    }

    /// <summary>
    /// Создать новый объект линию
    /// </summary>
    void CreateLine()
    {
        //Текущая линия
        CurrentLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);

        //Компоненты линии
        LineRenderer = CurrentLine.GetComponent<LineRenderer>();
        PolyCollider = CurrentLine.GetComponent<PolygonCollider2D>();

        //Установка начальных позиций линии
        FingerPositions.Clear();
        FingerPositions.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        FingerPositions.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        LineRenderer.SetPosition(0, FingerPositions[0]);
        LineRenderer.SetPosition(1, FingerPositions[1]);

        //Настройка коллайдера линии
        PolyCollider.points = FingerPositions.ToArray();
    }

    /// <summary>
    /// Добавление новых точек в форму машины
    /// </summary>
    /// <param name="newFingerPos">Новая точка</param>
    void UpdateLine(Vector2 newFingerPos)
    { 
        //Если точка слишком близко к предыдущей ничего не делать
        if (Vector2.Distance(newFingerPos, FingerPositions[FingerPositions.Count - 1]) < 1f)
            return;
        //Если расстояние между новой точкой и первой точкой линии меньше единицы - замкнуть линию
        if (Vector2.Distance(FingerPositions[0], newFingerPos) < 1f && FingerPositions.Count > 1)
        {
            //Замыкание линии
            LineRenderer.loop = true;

            //Переход к добавлению колес
            CurrentBuildMode = BuildModes.DrawingWheels;

            //Расчет и обновление массы машины
            CalcCarMass();
            CurrentLine.GetComponent<Rigidbody2D>().mass = CarMass * 0.01f;

            //Упрощение формы линии для сглаживания углов
            CurrentLine.GetComponent<LineRenderer>().Simplify(1);

            //Задержка в 300мс для завершения обработки
            Thread.Sleep(300);

            return;
        }

        //Добавление новой точки
        FingerPositions.Add(newFingerPos);
        LineRenderer.positionCount++;
        LineRenderer.SetPosition(LineRenderer.positionCount - 1, newFingerPos);

        //Обновление точек коллайдера
        PolyCollider.points = FingerPositions.ToArray();
    }

    void DrawPointsOnCorners()
    {
        foreach(var p in FingerPositions)
        {

        }
    }

}

public partial class LineDraw : MonoBehaviour
{
    /// <summary>
    /// Расчёт массы построенной машины
    /// </summary>
    void CalcCarMass()
    {
        for (int i = 1; i < FingerPositions.Count; i++)
        {
            CarMass += Vector2.Distance(FingerPositions[i], FingerPositions[i - 1]);
        }
    }

    /// <summary>
    /// Следование камеры за х координатой машины
    /// </summary>
    void CamFollowCar()
    {
        carCamera.transform.position = CurrentLine.transform.position.x * Vector3.right + new Vector3(0, 0, -20);
    }
}
