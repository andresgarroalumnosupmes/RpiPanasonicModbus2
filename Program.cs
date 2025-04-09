// See https://aka.ms/new-console-template for more information

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets; //Import Sockets
using System.Text;
using EasyModbus; // Imports EasyModbusCore

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello, Panasonic-Modbus!");
        string ipAddress = "192.168.0.101";  // Modbus-PLC IP
        int port = 502;                      // Modbus TCP Port 

        // Creation of the client Modbus TCP
        ModbusClient modbusClient = new ModbusClient(ipAddress, port);
        try
        {
            // Connecting to Modbus Server
            modbusClient.Connect(); 
            Console.WriteLine($"Connecting a {ipAddress}:{port}");


            // Container Name of the LISTENER Point
            string nameContainerListener = "RpiTemperatureSensorModule"; 
            
            // Dns.GetHostAddresses gets IP address of the container in the Docker network.
            //Socket is EXPOSED in port 8888
            IPEndPoint ListernerPoint1 = new IPEndPoint(Dns.GetHostAddresses(nameContainerListener)[0], 8888);
            //

            // Create a SOCKET TCP/IP to SEND Plc DATA
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Connect to the LISTENER POINT
                sender.Connect(ListernerPoint1);    
                Console.WriteLine("Connected a {0}", sender.RemoteEndPoint);
                
                while(true)
                {
                    int startAddress = 0;  // Address of the Input register which will be read
                    int[] registers = modbusClient.ReadInputRegisters(startAddress, 1); // Reading 1 register

                    // Convert the integer to array of bytes
                    byte[] bytes = BitConverter.GetBytes(registers[0]);
                
                    // Send the data through the Socket
                    int bytesSent = sender.Send(bytes);

                    Console.WriteLine("SERVER SOCKET of the PLC Container SENDING!");
                    Console.WriteLine($"Date Time: {DateTime.Now} - Value of Register {startAddress}: {registers[0]}");
                    //Console.WriteLine("Data sent: {0}", registers[0]); 
                    
                    // Wait for 3 seconds before the next reading 
                    Thread.Sleep(3000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            //If there is a error in the loop and the loop ends, the connection is closed
            finally
            {
                if (sender.Connected)
                {
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    Console.WriteLine("Connection closed for PLC Container.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        // Closing the connection regardeless of if there was an exceptsion or not
         finally
        {
            modbusClient.Disconnect();// Closing connection
            Console.WriteLine("Connection closed.");
        }
    }
}



