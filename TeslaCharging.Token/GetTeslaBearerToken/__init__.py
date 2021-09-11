import logging
import azure.functions as func

from .auth import get_bearer_token

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    req_body = req.get_json()
    email = req_body.get('email')
    password = req_body.get('password')

    if email:
        access_token = get_bearer_token(email, password)
        return func.HttpResponse(access_token)
    else:
        return func.HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
             status_code=200
        )
