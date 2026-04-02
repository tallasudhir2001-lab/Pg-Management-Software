import 'dart:io';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';

/// Mobile/Desktop: allow self-signed HTTPS certificates in development
void configureCertificateBypass(Dio dio) {
  (dio.httpClientAdapter as IOHttpClientAdapter).createHttpClient = () {
    final client = HttpClient();
    client.badCertificateCallback = (cert, host, port) => true;
    return client;
  };
}
