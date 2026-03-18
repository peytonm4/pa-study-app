#!/usr/bin/env python3
"""
Stub lecture extractor script.
Usage: python3 lecture_extractor.py <input_json_path>
Prints structured sections JSON to stdout.
"""
import sys, json


def main():
    output = {
        "sections": [
            {
                "level": 1,
                "heading": "Overview",
                "content": "Stub overview content for testing purposes.",
                "pages": [1, 2],
                "figures": []
            },
            {
                "level": 2,
                "heading": "Key Concepts",
                "content": "Stub key concepts content.",
                "pages": [3],
                "figures": ["stub-fig-1"]
            }
        ],
        "docx_filename": "stub_lecture.docx"
    }
    print(json.dumps(output))


if __name__ == "__main__":
    main()
