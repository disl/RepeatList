#!/usr/bin/env python3
"""
Obfuscate API keys with XOR + Base64 for use in SecretVault.cs.

Usage:
    python obfuscate_key.py <plaintext-key>

The passphrase must match SecretVault.Passphrase.
After rotation: replace the encoded value in SecretVault.cs and rebuild.
"""

import base64
import sys

PASSPHRASE = "RepeatList_v1_Salt#2024"


def obfuscate(plaintext: str) -> str:
    passphrase_bytes = PASSPHRASE.encode("utf-8")
    pt_bytes = plaintext.encode("utf-8")
    result = bytearray(len(pt_bytes))

    for i, b in enumerate(pt_bytes):
        result[i] = b ^ passphrase_bytes[i % len(passphrase_bytes)]

    return base64.b64encode(result).decode("ascii")


def deobfuscate(encoded: str) -> str:
    passphrase_bytes = PASSPHRASE.encode("utf-8")
    data = base64.b64decode(encoded)
    result = bytearray(len(data))

    for i, b in enumerate(data):
        result[i] = b ^ passphrase_bytes[i % len(passphrase_bytes)]

    return result.decode("utf-8")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python obfuscate_key.py <plaintext-key>")
        print("  or:  python obfuscate_key.py --decode <encoded-value>")
        sys.exit(1)

    if sys.argv[1] == "--decode":
        if len(sys.argv) < 3:
            print("Usage: python obfuscate_key.py --decode <encoded-value>")
            sys.exit(1)
        print(f"Decoded: {deobfuscate(sys.argv[2])}")
    else:
        encoded = obfuscate(sys.argv[1])
        print(f"Encoded: {encoded}")
        print(f"Add to SecretVault.cs:\nprivate const string NewKeyEncoded = \"{encoded}\";")
