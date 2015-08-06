using System;
using Amqp;

namespace HelloWorld
{
  class HelloWorld
  {
    static void Main(string[] args)
    {
      string brokerUrl = "amqp://localhost:5672";
      string address   = "my_queue";

      Address    brokerAddr = new Address(brokerUrl);
      Connection connection = new Connection(brokerAddr);
      Session    session    = new Session(connection);

      SenderLink   sender   = new   SenderLink(session, "sender",   address);
      ReceiverLink receiver = new ReceiverLink(session, "receiver", address);

      Message helloOut = new Message("Hello World!");
      sender.Send(helloOut);

      Message helloIn = receiver.Receive();
      receiver.Accept(helloIn);

      Console.WriteLine(helloIn.Body.ToString());

      receiver.Close();
      sender.Close();
      session.Close();
      connection.Close();
    }
  }
}
