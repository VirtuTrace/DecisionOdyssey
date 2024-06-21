import random

import requests

FIRST_NAMES = [ "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth" ]
LAST_NAMES = [ "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor" ]
EMAIL_PROVIDERS = [ "gmail.com", "yahoo.com", "hotmail.com", "aol.com", "outlook.com" ]

def main():
    session = requests.Session()
    for first_name in FIRST_NAMES:
        for last_name in LAST_NAMES:
            add_user(first_name, last_name, session)

def add_user(first_name: str, last_name: str, session: requests.Session):
    seed = random.randint(0, 2**32 - 1)
    email_provider = random.choice(EMAIL_PROVIDERS)
    email = f"{first_name}{last_name}{seed}@{email_provider}"
    payload = {
        "FirstName": first_name,
        "LastName": last_name,
        "Email": email,
        "Password": email
    }

    response = session.post("https://localhost:7280/api/users/register", json=payload, verify=False)
    if not response.ok:
        print(f"Failed to add user {first_name} {last_name} with email {email}")

if __name__ == "__main__":
    main()
