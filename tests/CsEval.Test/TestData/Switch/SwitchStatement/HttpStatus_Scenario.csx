{
    var statusCode = 404;
    var message = "";
    switch (statusCode) {
        case 200:
            message = "OK";
            break;
        case 201:
            message = "Created";
            break;
        case 400:
            message = "Bad Request";
            break;
        case 401:
            message = "Unauthorized";
            break;
        case 403:
            message = "Forbidden";
            break;
        case 404:
            message = "Not Found";
            break;
        case 500:
            message = "Internal Server Error";
            break;
        default:
            message = "Unknown Status";
            break;
    }
    return message;
}
