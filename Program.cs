// See https://aka.ms/new-console-template for more information


using System;
using System.Threading;
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
            modbusClient.Connect(); // Connecting to Modbus Server
            Console.WriteLine($"Connecting a {ipAddress}:{port}");
                while(true)
                {
                    int startAddress = 0;  // Address of the Input register which will be read
                    int[] registers = modbusClient.ReadInputRegisters(startAddress, 1); // Reading 1 register

                    Console.WriteLine($"Date Time: {DateTime.Now} - Value of Register {startAddress}: {registers[0]}");
                    Console.WriteLine($"Time sleep 2s. Dos segundos");
                    // wait for 2 seconds before the next reading 
                    Thread.Sleep(2000);
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



