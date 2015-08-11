# AMQP.Net Lite Client, A-MQ Broker, and TLS/SSL

In this post I'm going to show how to encrypt the communications between the Red Hat JBoss A-MQ (A-MQ) broker and the AMQP.Net Lite (Lite) client. 

  * **Disclaimer**: This article is a demonstration of the security concepts with concrete examples and recipes you can use for testing. It is not a manual for securing a public installation large or small.

## Overview

There are two levels of authentication that can be performed:

 * [TLS Handshake](https://technet.microsoft.com/en-us/library/Cc783349\(v=WS10\).aspx)
 * TLS Handshake with Client Certificate Request

### TLS Handshake

TLS provides an authenticated and secured communications channel. For simple TLS the broker must present a certificate that is signed by a certificate authority (CA) that the client trusts.

### TLS Handshake with Client Certificate Request

The broker must always send a certificate to the client to start the secure channel with the TLS Handshake. Using a client certificate request the broker can demand that the client authenticate itself by returning a certificate to the broker. The client's identity is securely embedded in the client certificate and by carefully controlling who gets the client certificates a system administrator may control who gets access to resources in the A-MQ broker. 

This is useful for messaging environments where the broker must confirm the identity of the client with stronger authentication than just a username and password. 

## Security Providers

The Lite client and the A-MQ broker use different security providers for creating and managing SSL/TLS connections.

 * Lite uses [SChannel](https://msdn.microsoft.com/en-us/library/windows/desktop/ms678421%28v=vs.85%29.aspx)
 * A-MQ uses [javax.net.ssl](http://docs.oracle.com/cd/B19306_01/java.102/b14355/sslthin.htm#BABCBGDB).

In either case we must configure the security provider with the proper certificates before the provider can set up secure SSL/TLS communication channels.

## Certificate Generation Tools

In this example we are going to generate all the certificates locally. There are plenty of certificate generation tools from which to choose:

 * OpenSSL
 * Java Keytool
 * IBM KeyMan
 * Microsoft Makecert
 * Portecle
 * and more ...


For this article I choose to use [Java Keytool](https://docs.oracle.com/javase/8/docs/technotes/tools/unix/keytool.html) from [Oracle](http://www.oracle.com/technetwork/java/javase/downloads/index.html) to run on a Windows host. To get *keytool*, install Java Platform, Standard Edition (Java SE), and add the kit to your execution environment. These settings add version jdk1.7.0_79 to a process environment:

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
 * Create a trust store for the broker; import CA certificate into it

### Configuration script changes required for your setup

In this script you need to adjust some variables to match the environment in which the scripts will be used. 

 * CERT_LOC The folder where all the certificates are generated and stored. The default is *.\keys* and that is usually adequate for test purposes.
 * CA_CN The name of the self-signed certificate authority this script creates. There are no limits on this value but use some common sense and don't create a name that conflicts with any public certificate authority.
 * BROKER_CN This name must be that public hostname of the broker system. This is the name in the connection URL that the client uses to access the broker.
 * CLIENT_CN This is the client's username used in the client certificate.

For more complicated installations you may want more descriptive file names. The file path suffixes (.keystore, .truststore, .crt, .csr, .p12) are common for the content illustrated here.

### Certificate generation script

Generate your certificate files with [this script](https://people.apache.org/~chug/blog/wordpress-02/gen-win-ssl-certs.bat.html). Remember to edit CA\_CN, BROKER\_CN, and CLIENT\_CN to fit your environment.

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

Assume that you have run the generation script in the broker's root folder. The certificates will be in folder *.\keys*. In the broker configuration file the key stores are loaded by adding these lines to file *.\conf\activemq.xml*.

    <sslContext>
      <sslContext
        keyStore="${activemq.conf}/../keys/broker-jks.keystore" keyStorePassword="password"
        trustStore="${activemq.conf}/../keys/broker-jks.truststore" trustStorePassword="password"/>
    </sslContext>

### Creating the AMQPS TLS Transport Connector for TLS Handshake Only

In file *.\conf/activemq.xml* add the A-MQ TLS/SSL AMQP connector

    <transportConnector name="amqps" uri="amqp+ssl://0.0.0.0:5671?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600"/>


### Creating the AMQPS TLS Transport Connector for TLS Handshake and Client Certificate

In file *.\conf\activemq.xml* add a qualifier to the AMQPS transport connector

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

Before a client will return a certificate to the broker, the AMQP.Net Lite library must be told which certificates to use. The client certificate file *client.crt* is added to the list of certificates to be used during SChannel connection startup. 

    factory.SSL.ClientCertificates.Add(
        X509Certificate.CreateFromCertFile(certfile));

A complete example is found in [HelloWorld-client-certs.cs](https://people.apache.org/~chug/blog/wordpress-02/HelloWorld-client-certs.cs.html). This source file and the supporting project files are available in the SDK.

## Troubleshooting

My experience getting a setup to work usually involves several permission and connectivity issues. 

 * Check that the broker process has permission to listen on AMQPS port 5671.
 * Make sure your broker host system has AMQPS port 5671 open. This may involve both Windows firewall and third party security software.
 * Check that the rest of your infrastructure allows traffic to port 5671, too. 

Still can't connect? Fortunately, both the client and the broker have built-in facilities for troubleshooting SSL/TLS issues.

### AMQP.Net Lite

The Lite library uses SChannel to open the secure connection. When any SSL/TLS error happens the caller is only informed of "SSL Handshake failed". Detailed logging must be enabled through a registry setting. See [How to enable Schannel event logging in IIS](https://support.microsoft.com/en-us/kb/260729) for detailed instructions and the usual warnings about registry modification. Essentially this registry key has values which are logically ORed together:

    HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\SecurityProviders\SCHANNEL

    Value Name: EventLogging
    Data Type: REG_DWORD

    Value  Description
    =====  ===========
    0x0    Do not log
    0x1    Log error messages
    0x2    Log warnings
    0x4    Log information and success events

The Lite library has internal event logging but in the case of an SSL/TLS handshake failure Lite does not receive detailed event information from SChannel.

### A-MQ

Logging in the Java SSL code may enabled by starting the broker with

    -Djavax.net.debug=ssl

The [Red Hat JBoss A-MQ broker](https://access.redhat.com/documentation/en-US/Red_Hat_JBoss_A-MQ/6.2/index.html) is fully supported by Red Hat.

## Conclusion

With the proper certificates AMQP.Net Lite clients may use TLS Handshake encrypted channels when communicating with Red Hat JBoss A-MQ brokers. Advanced authentication may be performed with TLS Client Certificates.

Happy Messaging!
