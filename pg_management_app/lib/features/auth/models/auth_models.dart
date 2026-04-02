class LoginRequest {
  final String userNameOrEmail;
  final String password;

  LoginRequest({required this.userNameOrEmail, required this.password});

  Map<String, dynamic> toJson() => {
        'userNameOrEmail': userNameOrEmail,
        'password': password,
      };
}

class LoginResponse {
  final bool? isAdmin;
  final String? token;
  final String? refreshToken;
  final bool? requiresPgSelection;
  final String? tempToken;
  final List<PgOption>? pgs;

  LoginResponse({
    this.isAdmin,
    this.token,
    this.refreshToken,
    this.requiresPgSelection,
    this.tempToken,
    this.pgs,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      isAdmin: json['isAdmin'],
      token: json['token'],
      refreshToken: json['refreshToken'],
      requiresPgSelection: json['requirespgSelection'],
      tempToken: json['tempToken'],
      pgs: json['pgs'] != null
          ? (json['pgs'] as List).map((e) => PgOption.fromJson(e)).toList()
          : null,
    );
  }
}

class PgOption {
  final String pgId;
  final String pgName;
  final String role;

  PgOption({required this.pgId, required this.pgName, required this.role});

  factory PgOption.fromJson(Map<String, dynamic> json) {
    return PgOption(
      pgId: json['pgId'],
      pgName: json['pgName'],
      role: json['role'],
    );
  }
}

class SelectPgRequest {
  final String pgId;

  SelectPgRequest({required this.pgId});

  Map<String, dynamic> toJson() => {'pgId': pgId};
}
