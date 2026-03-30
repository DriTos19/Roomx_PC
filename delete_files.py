import os

files = [
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Logic\FBXLoader.cs',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Inventory\FBXImportUI.cs',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTester.cs',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTest.cs',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\FBX_IMPORT_SETUP.md',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\FBX_IMPORT_TESTING.md',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBX_IMPORT_README.md',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Logic\FBXLoader.cs.meta',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\Inventory\FBXImportUI.cs.meta',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTester.cs.meta',
    r'C:\Users\drini\OneDrive\Documents\GitHub\Roomx_PC\Assets\Scripts\FBXImportTest.cs.meta',
]

for f in files:
    if os.path.exists(f):
        os.remove(f)
        print(f'DELETED: {f}')
    else:
        print(f'NOT FOUND: {f}')
