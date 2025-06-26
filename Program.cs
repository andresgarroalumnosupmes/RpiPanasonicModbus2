using System;
using System.Threading;
using EasyModbus; // Packet for Modbus connection - Import EasyModbusCore. Establish communication to PLC PANASONIC 
using MQTTnet; //Packet for MQTT connection
using MQTTnet.Client;
using System.Text.Json;
using System.Text;
class Program
{
    //Var to store the newscale from Mqtt broker and share between task
    static int iMqttNewScalePayload = 0;
    static void Main()
    {
        Console.WriteLine("Hello, Panasonic-Modbus!");
        string ipAddress = "192.168.0.101";  // Modbus-PLC IP
        int port = 502;                      // Modbus TCP Port 

        // Creation of the client Modbus TCP
        ModbusClient modbusClient = new ModbusClient(ipAddress, port);
		
		//.WithTcpServer("localhost", 1884) //tested ok window Docker 
        // setting MQTT
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId($"ModbusMqttClient-{Guid.NewGuid()}")
            .WithTcpServer("172.17.0.3", 1884)
            .Build();
        
        try
        {
            /////////////////////////////////////////////////////////////////////////////////////////
            // Connect to Modbus
            Console.WriteLine($"Connecting a {ipAddress}:{port}");
            modbusClient.Connect(); // Connecting to Modbus Server
            Console.WriteLine("Modbus Connection established");
            /////////////////////////////////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////////////////////////////////
            // MQTT handlers
            
            // Register handlers before connecting
            mqttClient.ConnectedAsync += async e =>
            {
                Console.WriteLine("MQTT Connection established");
                // plant01/plc01/modbus/sensor01/temperature
                await mqttClient.SubscribeAsync("plant01/plc01/modbus/sensor01/newscale"); //plant01/#
            };

            //Handler informs if there was a discconnection from broker
            mqttClient.DisconnectedAsync += e =>
            {
                Console.WriteLine("Connection closed.");
                return Task.CompletedTask;
            };

            //Handler to get the Mqtt paylodad from the subscrption, topic:  plant01/plc01/modbus/sensor01/newscale
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string topic = e.ApplicationMessage.Topic;
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                Console.WriteLine($"Message received with the topic: {topic}");
                Console.WriteLine($"Payload (JSON): {payload}\n");

                iMqttNewScalePayload = int.Parse(payload);

                int startAddressHR = 0;  // Address of the Holding register which will be written                    
                modbusClient.WriteSingleRegister(startAddressHR, iMqttNewScalePayload); // Modbus Dir 40001 = index 0.  Writting 1 register
                Console.WriteLine($"Writting Modbus Holding Register. Date Time: {DateTime.Now} - Value of Register {startAddressHR}: {iMqttNewScalePayload}");

                //Check it using: mosquitto_pub -h localhost -p 1884 -t plant01/plc01/modbus/sensor01/newscale -m "2"
                return Task.CompletedTask; 
            }; 

            // Connect to MQTT
            Console.WriteLine("Connecting to broker MQTT...");
            mqttClient.ConnectAsync(mqttOptions).Wait();
            //Console.WriteLine("MQTT Connection established");
            /////////////////////////////////////////////////////////////////////////////////////////

            while(true)
            {
                try
                {
                    //MODBUS reading fromm Input registers and writting to Holding registers
                    int startAddressIR = 0;  // Address of the Input register which will be read
                    int[] registers = modbusClient.ReadInputRegisters(startAddressIR, 2); // Modbus Dir 10001 = index 0.   Reading 2 register
                    int registerTempValue = registers[0];
                    int registerScaleValue = registers[1];
                    Console.WriteLine($"Reading Modbus Input Register. Date Time: {DateTime.Now} - Value of Register {startAddressIR}: {registerTempValue} - Value of Register {startAddressIR+1}: {registerScaleValue}");
                    
                    //int startAddressHR = 0;  // Address of the Holding register which will be written                    
                    //modbusClient.WriteSingleRegister(startAddressHR, iMqttNewScalePayload); // Modbus Dir 40001 = index 0.  Writting 1 register

                    // Creating MQTT message with the register data to publish Temp value and scale
                    var data = new
                    {
                        sensor_id="sensorTemperature01",
                        temp_value = registerTempValue,
                        temp_unit = "ºC",
                        scale_value = registerScaleValue,
                        scale_unit = "ºC/V",
                        timestamp = DateTime.Now.ToString("o")
                    };
                    string jsonPayload = JsonSerializer.Serialize(data);

                    // Build and publish an MQTT message
                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("plant01/plc01/modbus/sensor01/temperature")
                        .WithPayload(jsonPayload)
                        .Build();
                    
                    mqttClient.PublishAsync(mqttMessage).Wait();
                    Console.WriteLine($"Message published the topic: plant01/plc01/modbus/sensor01/temperature. Payload: {jsonPayload}");
                    //check publising using: mosquitto_sub -h localhost -p 1884 -v -t plant01/plc01/modbus/sensor01/temperature
                    //////////////////////////////////////////////////////////////////////////////////////////////
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during reading Modbus register or publishing MQTT message: {ex.Message}");
                }

                // wait for 2 seconds before the next reading
                int samplingTime= 3000;
                Console.WriteLine($"Sampling Time: {samplingTime}\n"); 
                Thread.Sleep(samplingTime);
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



