using System;
using System.Globalization;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace GPS
{
    public partial class MainWindow : Window
    {
        static SerialPort _serialPort;

        string googleMapsUrl = "https://www.google.com/maps/search/?api=1&query=";

        int delay = 2000;
        string outputData;
        string latitude; // szerokosc geograficzna
        string longtitude; // dlugosc geograficzna
        string time;
        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            InitializeComponent();
            _serialPort = new SerialPort();
        }

        // Odczytanie danych z modułu GPS
        private void GetData()
        {
            //przypisanie wszystkich wiadomości
            outputData = _serialPort.ReadExisting();

            //Podzielenie wiadomości na linie
            var splitedData = outputData.Split('$');

            //Pętla po odebranych danych
            foreach (var line in splitedData)
            {
                try
                {
                    if (line.Contains("GNGGA"))
                    {
                        string fetchedLatitude = "";
                        string fetchedLongitude = "";

                        //podzielenie tekstu na kawałki pomiędzy przecinkami
                        var info = line.Split(',');

                        //Czas w formacie hhmmss.ss
                        time = info[1];
                        //latitude geograficzna  ddmm.mmmm 
                        latitude = info[2];
                        //longtitude 
                        longtitude = info[4];

                        //Konwersja na double i przesunięcie kropki o 2 pozycje
                        double timeFromMessage = double.Parse(time, CultureInfo.InvariantCulture) / 100.0;
                        double longdec = double.Parse(longtitude, CultureInfo.InvariantCulture) / 100.0;
                        double latdec = double.Parse(latitude, CultureInfo.InvariantCulture) / 100.0;

                        if (info[3] == "S")
                        {
                            fetchedLatitude = "-";
                        }
                        if (info[5] == "W")
                        {
                            fetchedLongitude = "-";
                        }
                        var latSplit = Convert.ToString(latdec).Split('.');
                        var longSplit = Convert.ToString(longdec).Split('.');
                        var timeSplit = Convert.ToString(timeFromMessage).Split('.');

                        double seconds = double.Parse(timeSplit[1], CultureInfo.InvariantCulture);
                        double hoursAndMinutes = double.Parse(timeSplit[0],CultureInfo.InvariantCulture) / 100;
                        var hoursAndMinutesSplit = Convert.ToString(hoursAndMinutes).Split('.');

                        time = hoursAndMinutesSplit[0] + ":" + hoursAndMinutesSplit[1] + ":" + seconds.ToString("F2");

                        longdec = Convert.ToDouble("0." + longSplit[1], CultureInfo.InvariantCulture) * 100;
                        latdec = Convert.ToDouble("0." + latSplit[1], CultureInfo.InvariantCulture) * 100;

                        //Szerokość geograficzna w formacie XX YY.ZZZZ
                        latitude = fetchedLatitude + Convert.ToDouble(latSplit[0]).ToString() + " " + latdec.ToString();
                        //Długość geograficzna w formacie XX YY.ZZZZ
                        longtitude = fetchedLongitude + Convert.ToDouble(longSplit[0]).ToString() + " " + longdec.ToString();


                        LatitudeTextBox.Text = latitude;
                        LongitudeTextBox.Text = longtitude;
                        timeTextBox.Text = time;

                        NumberOfSatelitesTextBox.Text = info[6];
                        HeightAboveSeaLevelTextBox.Text = info[9] + " m";
                        SeparationTextBox.Text = info[11] + " m";
                    }
                }
                catch (Exception)
                { }
            }
        }

        // Połączenie z urządzeniem Bluetooth
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.PortName = PortNameTextBox.Text;
            _serialPort.BaudRate = 9600;
            _serialPort.Open();

            //Uruchomienie wątku
            Task.Run(() =>
            {
                while (true)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { GetData(); }));
                    Thread.Sleep(delay);
                }
            });
        }


        // Wskazanie na mapie aktualnej lokalizacji 
        private void ShowOnMapButton_Click(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigated += new NavigatedEventHandler(WebBrowser_Navigated);
            webBrowser.Navigate(googleMapsUrl + latitude + ", " + longtitude);
        }

        // Delegat czyszczenia komunikatów
        void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            HideJsScriptErrors((WebBrowser)sender);
        }

        // Ukrycie komunikatów JS
        public void HideJsScriptErrors(WebBrowser wb)
        {
            FieldInfo fld = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fld == null)
                return;
            object obj = fld.GetValue(wb);
            if (obj == null)
                return;

            obj.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, obj, new object[] { true });
        }

    }
}
