# FTP Upload in Windows 8 Store Apps / Windows 10 Universal Apps

UWP profile supports uploading and downloading content from HTTP servers using:

* `Windows.Networking.BackgroundTransfer` namespace
* `Windows.Web.Http.HttpClient` class
* `System.Net.Http.HttpClient` class
* `System.Net.HttpWebRequest` class

And downloading content from FTP servers using:

* `Windows.Networking.BackgroundTransfer` namespace

However, there is NO official API to upload content to FTP servers.

However, using `Windows.Networking.Sockets` it is easy to create an FTP client to do uploads.

On this GitHub project you will find a simple example of how to upload a file to an FTP server.

Features included:

* Connect to FTP server.
     * Using a user name and a password.
     * Using anonymous authentication.
* Upload of `byte[]` arrays.
* Download of `byte[]` arrays.

Pending features:

* Add SSL/TLS support. This will allow secure FTP downloads, a feature currently not supported in Windows Runtime.
* Add methods to upload or download System.IO.FileStream objects. Currently you have to convert to `byte[]`.
* Add methods to upload Windows.Storage.Streams.IInputStream objects. Currently you have to convert to `byte[]`.
* Add methods to download Windows.Storage.Streams.IOutputStream objects. Currently you have to convert from `byte[]`.

FILE TRANSFER PROTOCOL (FTP) specification: https://tools.ietf.org/html/rfc959

FTP Extensions for IPv6 and NATs: https://tools.ietf.org/html/rfc2428

Give it a try and leave your feedback.
