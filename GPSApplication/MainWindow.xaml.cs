using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GPS
{
    public partial class MainWindow : Window
    {
        static SerialPort _serialPort;

        string googleMapsUrl = "https://www.google.com/maps/search/?api=1&query=";

        int delay = 2000;
        string outputData;
        string szerokosc;
        string wysokosc;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            InitializeComponent();
            _serialPort = new SerialPort();
        }

        /// Odczytanie danych z modułu GPS
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

                        szerokosc = info[2];
                        wysokosc = info[4];


                        double longdec = double.Parse(wysokosc, CultureInfo.InvariantCulture) / 100.0;
                        double latdec = double.Parse(szerokosc, CultureInfo.InvariantCulture) / 100.0;
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

                        longdec = Convert.ToDouble("0." + longSplit[1], CultureInfo.InvariantCulture) * 100;
                        latdec = Convert.ToDouble("0." + latSplit[1], CultureInfo.InvariantCulture) * 100;

                        //Szerokość geograficzna w formacie XX YY.ZZZZ
                        szerokosc = fetchedLatitude + Convert.ToDouble(latSplit[0]).ToString() + " " + latdec.ToString();
                        //Długość geograficzna w formacie XX YY.ZZZZ
                        wysokosc = fetchedLongitude + Convert.ToDouble(longSplit[0]).ToString() + " " + longdec.ToString();


                        LatitudeTextBox.Text = szerokosc;
                        LongitudeTextBox.Text = wysokosc;

                        NumberOfSatelitesTextBox.Text = info[6];
                        HeightAboveSeaLevelTextBox.Text = info[8] + " m";
                        SeparationTextBox.Text = info[11] + " m";
                    }
                }
                catch (Exception)
                { }
            }
        }

        /// Połączenie z urządzeniem Bluetooth
        /// <param name="sender"></param>
        /// <param name="e"></param>
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


        /// Wskazanie na mapie aktualnej lokalizacji 
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowOnMapButton_Click(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigated += new NavigatedEventHandler(WebBrowser_Navigated);
            webBrowser.Navigate(googleMapsUrl + szerokosc + ", " + wysokosc);
        }

        /// Delegat czyszczenia komunikatów
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            HideJsScriptErrors((WebBrowser)sender);
        }

        /// Ukrycie komunikatów JS
        /// <param name="wb"></param>
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
