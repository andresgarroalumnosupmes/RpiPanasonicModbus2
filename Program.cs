// See https://aka.ms/new-console-template for more information


using System;
using System.Threading;
using EasyModbus; // Packet for Modbus connection - Import EasyModbusCore
using MQTTnet; //Packet for MQTT connection
using MQTTnet.Client;
using System.Text.Json;
class Program
{
    static void Main()
    {
        Console.WriteLine("Hello, Panasonic-Modbus!");
        string ipAddress = "192.168.0.101";  // Modbus-PLC IP
        int port = 502;                      // Modbus TCP Port 

        // Creation of the client Modbus TCP
        ModbusClient modbusClient = new ModbusClient(ipAddress, port);

        // setting MQTT
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId($"ModbusMqttClient-{Guid.NewGuid()}")
            .WithTcpServer("localhost", 1884)
            .Build();
        
        try
        {
            // Connect to Modbus
            Console.WriteLine($"Connecting a {ipAddress}:{port}");
            modbusClient.Connect(); // Connecting to Modbus Server
            Console.WriteLine("Modbus Connection established");

            // Connect to MQTT
            Console.WriteLine("Connecting to broker MQTT...");
            mqttClient.ConnectAsync(mqttOptions).Wait();
            Console.WriteLine("MQTT Connection established");

            while(true)
            {
                try
                {
                    int startAddress = 0;  // Address of the Input register which will be read
                    int[] registers = modbusClient.ReadInputRegisters(startAddress, 1); // Reading 1 register
                    int registerValue = registers[0];

                    Console.WriteLine($"Date Time: {DateTime.Now} - Value of Register {startAddress}: {registerValue}");

                    // Creating MQTT message with the register data 
                    var data = new
                    {
                        sensor_id="sensorTemperature01",
                        value = registerValue,
                        unit = "C",
                        timestamp = DateTime.Now.ToString("o")
                    };
                    string jsonPayload = JsonSerializer.Serialize(data);

                    // Build and publish an MQTT message
                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("plant01/plc01/modbus/sensor01/temperature")
                        .WithPayload(jsonPayload)
                        .Build();
                    
                    mqttClient.PublishAsync(mqttMessage).Wait();
                    Console.WriteLine($"Mensaje publicado: {jsonPayload}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during reading Modbus register or publishing MQTT message: {ex.Message}");
                }  
                
                // wait for 2 seconds before the next reading 
                Thread.Sleep(2000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
        }
        
        // Closing the connection regardeless of if there was an exceptsion or not
         finally
        {
            modbusClient.Disconnect();// Closing connection
            Console.WriteLine("Connection closed.");
            // Desconnect both clientes
            if (modbusClient.Connected)
            {
                modbusClient.Disconnect();
                Console.WriteLine("Disconnect from Modbus");
            }
            
            if (mqttClient.IsConnected)
            {
                mqttClient.DisconnectAsync().Wait();
                Console.WriteLine("Disconnect from MQTT");
            }
        }
    }
}





