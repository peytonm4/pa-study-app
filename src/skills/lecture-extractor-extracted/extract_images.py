#!/usr/bin/env python3
"""
Stub figure extraction script.
Usage: python3 extract_images.py <input_json_path>
Prints figure manifest JSON to stdout.
"""
import sys, json


def main():
    # In stub mode, ignore input and return deterministic output
    manifest = {
        "figures": [
            {
                "id": "stub-fig-1",
                "s3_key": "stub/fig1.png",
                "page": 1,
                "has_caption": True,
                "label_type": "Figure",
                "width": 800,
                "height": 600
            },
            {
                "id": "stub-fig-2",
                "s3_key": "stub/fig2.png",
                "page": 3,
                "has_caption": False,
                "label_type": None,
                "width": 400,
                "height": 300
            }
        ]
    }
    print(json.dumps(manifest))


if __name__ == "__main__":
    main()
