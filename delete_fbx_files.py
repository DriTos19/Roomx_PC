#!/usr/bin/env python3
import os
import sys

files_to_delete = [
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Logic\FBXLoader.cs",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Inventory\FBXImportUI.cs",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTester.cs",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTest.cs",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\FBX_IMPORT_SETUP.md",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\FBX_IMPORT_TESTING.md",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBX_IMPORT_README.md",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Logic\FBXLoader.cs.meta",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Inventory\FBXImportUI.cs.meta",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTester.cs.meta",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTest.cs.meta",
    r"C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\delete_files.py",
]

print("File Deletion Results:")
print("=" * 80)

for i, file_path in enumerate(files_to_delete, 1):
    if os.path.exists(file_path):
        try:
            os.remove(file_path)
            print(f"{i:2d}. DELETED: {file_path}")
        except Exception as e:
            print(f"{i:2d}. ERROR: {file_path} - {str(e)}")
    else:
        print(f"{i:2d}. NOT FOUND: {file_path}")

print("=" * 80)
