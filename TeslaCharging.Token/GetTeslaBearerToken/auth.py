import json, time
import requests
from requests.exceptions import HTTPError, Timeout
import urllib3
urllib3.disable_warnings() # For 'verify=False' SSL warning
import logging
import urllib
import os
import hashlib
import base64
from urllib.parse import urlparse
import re
import random
import string
import tempfile
from reportlab.graphics import renderPM
from svglib.svglib import svg2rlg
from twocaptcha import TwoCaptcha

def rand_str(chars=43):
    letters = string.ascii_lowercase + string.ascii_uppercase + string.digits + "-" + "_"
    return "".join(random.choice(letters) for i in range(chars))  

def solve_captcha(svg):
    with tempfile.NamedTemporaryFile(suffix='.svg', delete=False) as f:
        f.write(svg)
        drawing = svg2rlg(f.name)
        with tempfile.NamedTemporaryFile(suffix='.png', delete=False) as p:
           renderPM.drawToFile(drawing,p.name,fmt='PNG')
        solver = TwoCaptcha('287cf7967fa59fb6ac5ac4a10dbc92bd')
        return solver.normal(p.name)

def solve_recaptcha(sitekey, url):
    solver = TwoCaptcha('287cf7967fa59fb6ac5ac4a10dbc92bd')
    return solver.recaptcha(sitekey, url)

def get_bearer_token(email, password):
    # get login page
    session = requests.Session()
    verifier_bytes = os.urandom(32)
    challenge = base64.urlsafe_b64encode(verifier_bytes).rstrip(b'=')
    challenge_bytes = hashlib.sha256(challenge).digest()
    challengeSum = base64.urlsafe_b64encode(challenge_bytes).rstrip(b'=')

    getVars = {'client_id': 'ownerapi', 
                'code_challenge': challengeSum,
                'code_challenge_method' : "S256",
                'redirect_uri' : "https://auth.tesla.com/void/callback",
                'response_type' : "code",
                'scope' : "openid email offline_access",
                'state' : "tesla_charging"
    }
    url = os.environ["TeslaAuthUri"]

    auth_url = url + '?' + urllib.parse.urlencode(getVars)
    headers = {}
    resp = session.get(auth_url, headers=headers)

    csrf = re.search('name="_csrf".+value="([^"]+)"', resp.text).group(1)
    transaction_id = re.search('name="transaction_id".+value="([^"]+)"', resp.text).group(1)

    # get auth code
    data = {
        "_csrf": csrf,
        "_phase": "authenticate",
        "_process": "1",
        "transaction_id": transaction_id,
        "cancel": "",
        "identity": email,
        "credential": password,
    }
    resp = session.post(auth_url, headers=headers, data=data, allow_redirects=False)
    logging.info("Get Auth Code 1st post returns " + str(resp.status_code))

    ## handle captcha
    if resp.status_code != 302:
        if resp.text.find('captcha') > 0:
            logging.info("Get Auth Code captcha found")
            captcha_resp = session.get('https://auth.tesla.com/captcha')
            captcha = solve_captcha(captcha_resp.content)
            data = {
                "_csrf": csrf,
                "_phase": "authenticate",
                "_process": "1",
                "transaction_id": transaction_id,
                "cancel": "",
                "identity": email,
                "credential": password,
                "captcha": captcha['code']
            }
            resp = session.post(auth_url, headers=headers, data=data, allow_redirects=False)

    logging.info("Get Auth Code 2nd post returns " + str(resp.status_code))

    ## handle recaptcha
    if resp.status_code != 302:
        if resp.text.find('recaptcha') > 0:
            logging.info("Get Auth Code recaptcha found")    
            sitekey_index = resp.text.find('sitekey')
            sitekey= resp.text[sitekey_index+12:sitekey_index+52]
            recaptcha = solve_recaptcha(sitekey,url)
            data = {
                "_csrf": csrf,
                "_phase": "authenticate",
                "_process": "1",
                "transaction_id": transaction_id,
                "cancel": "",
                "identity": email,
                "credential": password,
                "captcha": captcha['code'],
                "recaptcha": recaptcha
            }
            resp = session.post(auth_url, headers=headers, data=data, allow_redirects=False)

    code_url = resp.headers["location"]
    parsed = urlparse(code_url)
    code = urllib.parse.parse_qs(parsed.query)['code']
   
    # swap code for bearer token
    payload = {
        "grant_type": "authorization_code",
        "client_id": "ownerapi",
        "code_verifier": rand_str(108),
        "code": code,
        "redirect_uri": "https://auth.tesla.com/void/callback",
    }

    resp = session.post(os.environ["TeslaTokenUri"], headers=headers, json=payload)
    access_token = resp.json()["access_token"]
    
    # swap bearer token for access token
    headers["authorization"] = "bearer " + access_token
    payload = {
        "grant_type": "urn:ietf:params:oauth:grant-type:jwt-bearer",
        "client_id": '81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384',
    }
    resp = session.post(os.environ["TeslaMotorsTokenUri"], headers=headers, json=payload)
    # owner_access_token = resp.json()["access_token"]

    return resp.text

