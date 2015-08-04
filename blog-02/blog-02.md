# AMQP.Net Lite Client, A-MQ Broker, and TLS/SSL

In this post I'm going to show how to encrypt your link so that your communications are secured. 

This is not a formula for securing a large, scalable, public installation. Rather it will demonstrate the concepts and give you some recipes you can use in your test setups to show that your broker and clients are using security credentials properly.

## Overview

There are two levels of authentication that can be performed:

 * [TLS Handshake](https://technet.microsoft.com/en-us/library/Cc783349\(v=WS10\).aspx)
 * TLS Handshake with Client Certificate Request

### TLS Handshake

TLS provides an authenticated and secured communications channel. For simple TLS the broker must present a certificate that is signed by a certificate authority (CA) that the client trusts.

### TLS Handshake with Client Certificate Request

The broker must always send a certificate to the client to start the secure channel with the TLS Handshake. Using a client certificate request the broker can demand that the client authenticate itself by sending a certificate to the broker. The client's identity is securely embedded in the client certificate and by carefully controlling who gets the client certificates a system administrator may control who gets access to resources in the A-MQ broker. 

This is useful for messaging environments such as a banking network where the broker must confirm the identity of the client with stronger authentication than just a username and password. 

## Security Providers

The AMQP.Net Lite Client and the Red Hat JBoss A-MQ Broker use different security providers for creating and managing SSL/TLS connections.

 * AMQP.Net Lite Client uses [SChannel](https://msdn.microsoft.com/en-us/library/windows/desktop/ms678421%28v=vs.85%29.aspx)
 * Red Hat JBoss A-MQ Broker uses [javax.net.ssl](http://docs.oracle.com/cd/B19306_01/java.102/b14355/sslthin.htm#BABCBGDB).

In either case we must configure the security provider with the proper certificates and then the provider sets up SSL/TLS for us.

## Certificate Generation Tools

So where do we get certificates? There are plenty of certificate generation tools from which to choose:

 * OpenSSL
 * Java Keytool
 * IBM KeyMan
 * Microsoft Makecert
 * Portecle
 * and more ...
</div>

For this article I choose to use [Java Keytool](https://docs.oracle.com/javase/8/docs/technotes/tools/unix/keytool.html) from [Oracle](http://www.oracle.com/technetwork/java/javase/downloads/index.html) to run on a Windows host. To get *keytool*, install Java Platform, Standard Edition (Java SE) and add the kit to your execution environment. These settings add version jdk1.7.0_79 to a process environment:

    set java_home=c:\program files\java\jdk1.7.0_79
    set      path=%path%;%java_home%\bin

## Generate a set of certificates

### Overview

The script generates all the certificates needed for TLS Handshake and Client Certificate exchange.
The steps to perform are:

 * Create a private, local Certificate Authority (the 'CA')
 * Create a key pair for the broker and sign it with CA
 * Import CA and broker certificates into broker keystore
 * Create a key pair for the client and sign it with CA
 * Import CA and client certificates into client keystore
 * Export client private key and certificate trust chain into a Personal Information Exchange
 * Create a trust store for the broker, import CA certificate into it

### Configuration script changes required for your setup

In this script you need to adjust some variables to match the environment in which the scripts will be used. 

 * CERT_LOC The folder where all the certificates are generated and stored.
 * CA_CN The name of the self-signed certificate authority this script creates. There are no limits on this value but use some common sense and don't create a name that conflicts with any public certificate authority.
 * BROKER_CN This name must be that public hostname of the broker system. This is the name in the connection URL that the client uses to access the broker.
 * CLIENT_CN This is the client's username used in the client certificate.

For more complicated installations you may want more descriptive file names. The file path suffixes (.keystore, .truststore, .crt, .csr, .p12) are common for the content illustrated here.

### Certificate generation script

TODO: BAT file syntax highlighting, please.

    REM  ------------------------------------------------------------------------------------
    REM  Copyright (c) Red Hat, Inc.
    REM  All rights reserved. 
    REM  
    REM  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
    REM  file except in compliance with the License. You may obtain a copy of the License at 
    REM  http://www.apache.org/licenses/LICENSE-2.0  
    REM  
    REM  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
    REM  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
    REM  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
    REM  NON-INFRINGEMENT. 
    REM 
    REM  See the Apache Version 2.0 License for specific language governing permissions and 
    REM  limitations under the License.
    REM  ------------------------------------------------------------------------------------
    REM
    REM file: gen-win-ssl-certs.bat
    REM date: 2015-07-01
    REM what: Procedure to create client auth certificate set for an ActiveMQ broker
    REM       and an AMQPNet.Lite client. "keytool" is supplied by jdk.
    REM       A user may change TODO: settings to match his setup.
    REM
    REM These files are created:
    REM broker-jks.keystore   BROKER_KEYSTORE
    REM                         ActiveMQ broker conf\activemq.xml broker sslContext.
    REM broker-jks.truststore BROKER_TRUSTSTORE
    REM                         ActiveMQ broker conf\activemq.xml broker sslContext.
    REM ca.crt                CA_CERT
    REM                         installed in client machine 
    REM                         Trusted Root Certification Authorities store
    REM client.p12            CLIENT_PRIVATE_KEYS
    REM                         installed in client machine Personal Certificates store.
    REM client.crt            CLIENT_CERT
    REM                         referred to/loaded by the C# application code.
    REM
    REM * The BROKER_KEYSTORE and BROKER_TRUSTSTORE do not need to be installed. The ActiveMQ
    REM   broker refers to these files in conf\activemq.xml.
    REM * CA_CERT and CLIENT_PRIVATE_KEYS are installed in the certificates stores 
    REM   of the client system.
    REM * CLIENT_CERT is used in creating an X509CertificateCollection. It is the certfile in:
    REM     ClientCertificates.Add(X509Certificate.CreateFromCertFile(certfile))
    REM
    REM The password for every store and certificate is "password".
    REM TODO: Use better passwords.
    
    REM All certificates are created in this subfolder
    REM TODO: Define where certificates are generated
    SET CERT_LOC=%CD%\certs
    
    REM Define the CN of the self-signed certificate authority.
    REM TODO: Define the CA's CN
    SET CA_CN=reallly-trust-me.org
    
    REM Define the CN of the broker.
    REM This is the fqdn or hostname of the machine on which the ActiveMQ broker is running.
    REM TODO: Define the broker CN:
    SET BROKER_CN=71.71.71.71
    
    REM Define the CN of the client. A client presenting this certificate will be authenticated
    REM as this user name in the ActiveMQ broker.
    REM TODO: Define the client CN
    SET CLIENT_CN=client
    
    REM Define file names
    SET          CA_KEYSTORE=ca-jks.keystore
    SET      BROKER_KEYSTORE=broker-jks.keystore
    SET      CLIENT_KEYSTORE=client-jks.keystore
    SET    BROKER_TRUSTSTORE=broker-jks.truststore
    SET              CA_CERT=ca.crt
    SET BROKER_CERT_SIGN_REQ=broker.csr
    SET          BROKER_CERT=broker.crt
    SET CLIENT_CERT_SIGN_REQ=client.csr
    SET          CLIENT_CERT=client.crt
    SET  CLIENT_PRIVATE_KEYS=client.p12
    
    REM Define some repeated patterns identifying the various stores
    SET     CA_STORE=-storetype jks -keystore     %CA_KEYSTORE% -storepass password
    SET BROKER_STORE=-storetype jks -keystore %BROKER_KEYSTORE% -storepass password
    SET CLIENT_STORE=-storetype jks -keystore %CLIENT_KEYSTORE% -storepass password
    SET KEY_PASSWORD=-keypass password
    
    REM Recreate entire cert folder
    rmdir /s /q %CERT_LOC%
    timeout /t 1 /nobreak > nul
    mkdir       %CERT_LOC%
    pushd       %CERT_LOC%
    
    REM Create the certificate authority CA
    REM -----------------------------------
    keytool %CA_STORE% %KEY_PASSWORD% -alias ca -genkey ^
            -dname "O=Reallly Trust Me Inc.,CN=%CA_CN%" -validity 9999 -ext bc:c=ca:true
    keytool %CA_STORE%                -alias ca -exportcert -rfc > %CA_CERT%
    
    REM Create a key pair for the broker and sign it with the CA
    REM --------------------------------------------------------
    keytool %BROKER_STORE% %KEY_PASSWORD% -alias broker -genkey -keyalg RSA -keysize 2048 ^
            -dname "O=Broker,CN=%BROKER_CN%" -validity 9999 -ext bc=ca:false -ext eku=serverAuth
    
    keytool %BROKER_STORE% -alias broker -certreq    -file %BROKER_CERT_SIGN_REQ%
    keytool %CA_STORE%     -alias ca -gencert -rfc -infile %BROKER_CERT_SIGN_REQ% ^
            -outfile %BROKER_CERT% -ext bc=ca:false -ext eku=serverAuth
    
    REM Import CA and broker certificates into broker keystore
    REM ------------------------------------------------------
    keytool %BROKER_STORE% %KEY_PASSWORD% -importcert -alias ca     -file     %CA_CERT% -noprompt
    keytool %BROKER_STORE% %KEY_PASSWORD% -importcert -alias broker -file %BROKER_CERT%
    
    REM Create a key pair for the client and sign it with the CA
    REM --------------------------------------------------------
    keytool %CLIENT_STORE% %KEY_PASSWORD% -alias client -genkey -keyalg RSA -keysize 2048 ^
            -dname "O=Client,CN=%CLIENT_CN%" -validity 9999 -ext bc=ca:false -ext eku=clientAuth
    
    keytool %CLIENT_STORE% -alias client -certreq    -file %CLIENT_CERT_SIGN_REQ%
    keytool %CA_STORE%     -alias ca -gencert -rfc -infile %CLIENT_CERT_SIGN_REQ% ^
            -outfile %CLIENT_CERT% -ext bc=ca:false -ext eku=clientAuth
    
    REM Import CA and client certs into client keystore
    REM -----------------------------------------------
    keytool %CLIENT_STORE% %KEY_PASSWORD% -importcert -alias ca     -file     %CA_CERT% -noprompt
    keytool %CLIENT_STORE% %KEY_PASSWORD% -importcert -alias client -file %CLIENT_CERT% -noprompt
    
    REM Export client private key and cert trust chain to Personal Information Exchange .p12
    REM ------------------------------------------------------------------------------------
    keytool -v -importkeystore -srckeystore %CLIENT_KEYSTORE% -srcstorepass password ^
            -srcalias client -destkeystore %CLIENT_PRIVATE_KEYS% -deststorepass password ^
    		-destkeypass password -deststoretype PKCS12 -destalias client
    
    REM Create a trust store for the broker, import the CA cert
    REM -------------------------------------------------------
    keytool -storetype jks -keystore %BROKER_TRUSTSTORE% -storepass password %KEY_PASSWORD% ^
            -importcert -alias     ca -file     %CA_CERT% -noprompt
    
    REM Make client cert legible in .NET
    REM --------------------------------
    dos2unix %CLIENT_CERT%
    
    REM Clean up
    REM --------
    del %CA_KEYSTORE%
    del %BROKER_CERT_SIGN_REQ%
    del %BROKER_CERT%
    del %CLIENT_CERT_SIGN_REQ%
    del %CLIENT_STORE%
    popd

### Distribute Certificates

On the broker system the broker configuration references files

    broker-jks.keystore
    broker-jks.truststore

On client systems that use only the TLS Handshake the client needs

    ca.crt

On client systems that use TLS Handshake and Client Certificate the client needs

    ca.crt
    client.crt
    client.p12

## Configuring the Broker

### Referencing the keystore and truststore files

There is no special installation required for the certificate files. They must be readable during broker start up.

Assume that you have run the generation script in the broker's root folder. The certificates will be in folder .\keys. In the broker configuration file the key stores are loaded by adding these lines to file *conf\activemq.xml*.

    <sslContext>
      <sslContext
        keyStore="${activemq.conf}/../keys/broker-jks.keystore" keyStorePassword="password"
        trustStore="${activemq.conf}/../keys/broker-jks.truststore" trustStorePassword="password"/>
    </sslContext>

### Creating the AMQPS TLS Transport Connector for TLS Handshake Only

In file *conf/activemq.xml* add the TLS/SSL AMQP connector

    <transportConnector name="amqp" uri="amqp://0.0.0.0:5672?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600"/>
    <transportConnector name="amqps" uri="amqp+ssl://0.0.0.0:5671?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600"/>


### Creating the AMQPS TLS Transport Connector for TLS Handshake and Client Certificate

In file *conf\activemq.xml* add a qualifier to the AMQPS transport connector

    <transportConnector name="amqps" uri="amqp+ssl://0.0.0.0:5671?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600&amp;needClientAuth=true"/>

Restart the broker. The TLS transport connector should be listening on port 5671.

## Configuring the Client

### Install the CA certificate

Client TLS Handshake is enabled by installing file *ca.crt* in the system's Trusted Root Certification Authorities store.

 * From an Administrator command prompt run the MMC Certificate Manager plugin: **certmgr.msc**
 * Expand the *Trusted Root Certification Authorities* folder on the left to expose *Certificates*
 * Right click *Certificates* and select *All Tasks -> Import...*
 * Click Next
 * Browse to select file *ca.crt*
 * Click Next
 * Select *Place all certificates in the following store*
 * Select Certificate store : *Trusted Root Certification Authorities*
 * Click Next
 * Click Finish

The certificate issued by *reallly-trust-me.org* should appear in the *Trusted Root Certification Authorities\Certificates* list.

### Use TLS Handshake Only

The client system is now configured to connect to the broker using TLS Handshake only. The client's source code must be adjusted to select the TLS transport channel. In *HelloWorld_simple.cs* the broker URL uses an unsecured channel:

    string broker = "amqp://guest:password@10.10.10.254:5672";

The client can connect to the secured channel by changing the scheme to *amqps* and the port number to *5671*

    string broker = "amqps://guest:password@10.10.10.254:5671";

### Use TLS Handshake and Client Certificates

In order to use TLS and client certficates then the certificates with the client's private keys must be imported into the proper certificate store on the client system.

 * From an Administrator command prompt run the MMC Certificate Manager plugin: **certmgr.msc**
 * Expand the *Personal* folder on the left to expose *Certificates*
 * Right click *Certificates* and select *All Tasks -> Import...*
 * Click Next
 * Click Browse
 * In the file type pulldown select *Personal Information Exchange (\*.pfx;\*.p12)*
 * Select file *client.p12* and press Open
 * Click Next
 * Type in the password for the private key: **password**. Accept default import options.
 * Click Next
 * Select *Place all certificates in the following store*
 * Select Certificate store : *Personal*
 * Click Next
 * Click Finish

### Hello World Example Using Client Certificates

Before a client will return a certificate to the broker, the AMQP.Net Lite library must be told which certificates to use. In the current release of AMQP.Net Lite this may be done using the async task methods shown in the following example. This example is available in Red Hat JBoss A-MQ AMQP.Net Lite Client SDK in *HelloWorld-client-certs.cs*.

     1 //  ------------------------------------------------------------------------------------
     2 //  Copyright (c) 2015 Red Hat, Inc.
     3 //  All rights reserved.
     4 //
     5 //  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this
     6 //  file except in compliance with the License. You may obtain a copy of the License at
     7 //  http://www.apache.org/licenses/LICENSE-2.0
     8 //
     9 //  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
    10 //  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR
    11 //  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
    12 //  NON-INFRINGEMENT.
    13 //
    14 //  See the Apache Version 2.0 License for specific language governing permissions and
    15 //  limitations under the License.
    16 //  ------------------------------------------------------------------------------------
    17 
    18 //
    19 // HelloWorld-client-certs
    20 //
    21 // Command line:
    22 //   HelloWorld-client-certs [brokerUrl [brokerEndpointAddress]]
    23 //
    24 using System;
    25 using System.Linq;
    26 using Amqp;
    27 using Amqp.Framing;
    28 using Amqp.Types;
    29 using System.Security;
    30 using System.Security.Cryptography;
    31 using System.Security.Cryptography.X509Certificates;
    32 using System.Security.Permissions;
    33 using System.Threading;
    34 using System.Threading.Tasks;
    35 
    36 namespace HelloWorld_simple
    37 {
    38   class HelloWorld_simple
    39   {
    40     static async Task<int> SslConnectionTestAsync(string brokerUrl, string address, string certfile)
    41     {
    42       try
    43       {
    44         ConnectionFactory factory = new ConnectionFactory();
    45         factory.TCP.NoDelay = true;
    46         factory.TCP.SendBufferSize = 16 * 1024;
    47         factory.TCP.SendTimeout = 30000;
    48         factory.TCP.ReceiveBufferSize = 16 * 1024;
    49         factory.TCP.ReceiveTimeout = 30000;
    50           
    51         factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
    52         factory.SSL.ClientCertificates.Add(X509Certificate.CreateFromCertFile(certfile));
    53         factory.SSL.CheckCertificateRevocation = false;
    54           
    55         factory.AMQP.MaxFrameSize = 64 * 1024;
    56         factory.AMQP.HostName = "host.example.com";
    57         factory.AMQP.ContainerId = "amq.topic";
    58            
    59         Address sslAddress = new Address(brokerUrl);
    60         Connection connection = await factory.CreateAsync(sslAddress);
    61 
    62         Session session = new Session(connection);
    63         SenderLink sender = new SenderLink(session, "sender1", address);
    64         ReceiverLink receiver = new ReceiverLink(session, "helloworld-receiver", address);
    65            
    66         Message helloOut = new Message("Hello - using client cert");
    67         await sender.SendAsync(helloOut);
    68 
    69         Message helloIn = await receiver.ReceiveAsync();
    70         receiver.Accept(helloIn);
    71 
    72         await connection.CloseAsync();
    73 
    74         Console.WriteLine("{0}", helloIn.Body.ToString());
    75 
    76         Console.WriteLine("Press enter key to exit...");
    77         Console.ReadLine();
    78         return 0;
    79       }
    80       catch (Exception e)
    81       {
    82         Console.WriteLine("Exception {0}.", e);
    83         return 1;
    84       }
    85     }
    86 
    87     static void Main(string[] args)
    88     {
    89       string broker = args.Length >= 1 ? args[0] : "amqps://client:password@host.example.com:5671";
    90       string address = args.Length >= 2 ? args[1] : "amq.topic";
    91       Task<int> task = SslConnectionTestAsync(broker, address, "D:\\FULL\\PATH\\TO\\client.crt");
    92       task.Wait();
    93     }
    94   }
    95 }

In this example the client certificate file *client.crt* is referenced in Line 91. Later (Line 52) the file is added to the list of certificates to be used during SChannel connection startup. 

## Conclusion

With the proper certificates AMQP.Net Lite clients may use TLS Handshake encrypted channels when communicating with Red Hat JBoss A-MQ brokers. Advanced authentication may be performed with TLS Client Certificates.

Happy Messaging!
