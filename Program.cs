using System;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace ApexSystemMonitor
{
    class Program
    {
        private static GameSenseClient? _gameSense;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Démarrage du Moniteur Système pour Apex 7 TKL...");
            Console.WriteLine("Note : LibreHardwareMonitor requiert souvent les droits d'Administrateur pour lire les températures et la charge GPU.");

            _gameSense = new GameSenseClient();
            bool isInitialized = await _gameSense.InitializeAsync();

            if (!isInitialized)
            {
                Console.WriteLine("Impossible d'initialiser GameSense. L'application va se fermer.");
                return;
            }

            // Gérer la fermeture de l'application (Ctrl+C)
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Empêche la fermeture immédiate
                _isRunning = false;
            };

            // 1. Enregistrement du jeu
            await _gameSense.RegisterGameAsync();

            // 2. Configuration de l'affichage OLED
            await _gameSense.BindEventAsync();

            // 3. Initialiser LibreHardwareMonitor
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());



            Console.WriteLine("Envoi des données en cours... (Appuyez sur Ctrl+C pour quitter)");

            while (_isRunning)
            {
                // Mettre à jour les capteurs
                foreach (IHardware hardware in computer.Hardware)
                {
                    hardware.Update();
                }

                string cpuLoadStr = "N/A";
                string cpuTempStr = "N/A";
                string gpuLoadStr = "N/A";
                string ramUsageStr = "N/A";

                float maxGpuLoad = 0;

                foreach (IHardware hardware in computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            // Charge globale du CPU
                            if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total"))
                            {
                                cpuLoadStr = $"{sensor.Value:00}%";
                            }
                            // Température du CPU
                            if (sensor.SensorType == SensorType.Temperature && (sensor.Name.Contains("Package") || sensor.Name.Contains("Tdie") || sensor.Name.Contains("Core Average")))
                            {
                                cpuTempStr = $"{sensor.Value:00}°C";
                            }
                        }
                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Name == "D3D 3D")
                            {
                                // On prend la charge GPU la plus élevée si on a plusieurs GPU (ex: Intel intégré + NVIDIA dédiée)
                                float currentGpuLoad = sensor.Value.GetValueOrDefault();
                                if (currentGpuLoad >= maxGpuLoad)
                                {
                                    maxGpuLoad = currentGpuLoad;
                                    gpuLoadStr = $"{maxGpuLoad:00}%";
                                }
                            }
                        }
                    }
                    else if (hardware.HardwareType == HardwareType.Memory)
                    {
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Name == "Memory")
                            {
                                ramUsageStr = $"{sensor.Value:00}%";
                            }
                        }
                    }
                }

                // Formatage du texte pour l'écran OLED (fusionné sur 2 lignes pour tout voir)
                string line1 = $"CPU:{cpuLoadStr} {cpuTempStr}";
                string line2 = $"GPU:{gpuLoadStr} RAM:{ramUsageStr}";

                await _gameSense.SendStatsAsync(line1, line2);

                // Attente de 5 secondes (pour les tests)
                await Task.Delay(5000);
            }

            // Nettoyage à la fermeture
            computer.Close();
            await _gameSense.StopGameAsync();
            Console.WriteLine("Application fermée proprement.");
        }
    }

    // Visiteur requis par LibreHardwareMonitor pour parcourir les composants et sous-composants
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
