# This file performs the installation step once the build is complete.
# Installation primarily involves copying the build results to the
# IronPython site-packages directory.

import os
import sys
import shutil
import tempfile
from os.path import dirname, isdir, isfile, join

src_dir = join(os.getcwd(), "numpy")


def check_ignore_dir(directory, dirs):
    for d in dirs:
        if directory.startswith(d):
            return True
    return False

def install():
    print "INSTALLING ..."
    sp_dir = join(sys.prefix, r'Lib\site-packages')
    numpy_dir = src_dir
    dll_dir = join(sys.prefix, 'DLLs')
    if not isdir(dll_dir):
        os.mkdir(dll_dir)

    ignore_pys = ["setup.py", "iron_install.py", "iron_setup.py", "iron_egg.py",
            "pavement.py", "setupegg.py", "setupscons.py", "setupsconsegg.py"]
    ignore_pys_dirs = [ r".\benchmarks", r".\doc", r".\libndarray", r".\tools"]
    ignore_dirs = [r".\.git"]
    ignore_libs = ["Microsoft.Scripting.dll",
                   "Microsoft.Scripting.Metadata.dll",
                   "Microsoft.Dynamic.dll",
                   "IronPython.dll",
                   "IronPython.Modules.dll"]

    # Recursively walk the directory tree and copy all .py files into the
    # site-packages directory and all .dll files into DLLs.
    for root, dirs, files in os.walk("."):
        if check_ignore_dir(root, ignore_dirs):
            continue
        for fn in files:
            rel_path = join(root, fn)
            #rel_path = abs_path[len(numpy_dir) + 1:]
            if fn.endswith('.py') and fn not in ignore_pys and not check_ignore_dir(root, ignore_pys_dirs):
                #print "abs_path = %s, relpath = %s" % ("", rel_path)
                dst_dir = dirname(join(sp_dir, rel_path))
                if not isdir(dst_dir):
                     os.makedirs(dst_dir)

                # Rename the _clr.py files to remove the _clr suffix.
                # Only used for IronPython.
                if fn.endswith('_clr.py'):
                    dst_file = join(dst_dir, fn[:-7] + ".py")
                else:
                    dst_file = join(dst_dir, fn)

                #print "Copy %s to %s" % (rel_path, dst_file)
                shutil.copy(rel_path, dst_file)
            elif fn.endswith('.dll') and fn not in ignore_libs:
                dst_file = join(dll_dir, fn)
                print rel_path
                if isfile(dst_file):
                    # Rename existing file because it is probably in use
                    # by the ipy command.
                    tmp_dir = tempfile.mkdtemp()
                    os.rename(dst_file, join(tmp_dir, fn))
                #print "Copy %s to %s" % (rel_path, dst_file)
                shutil.copy(rel_path, dst_file)

    write_config(join(sp_dir, r'numpy\__config__.py'))
    write_version(join(sp_dir, r'numpy\version.py'))


def write_config(path):
    fo = open(path, 'w')
    fo.write("""# this file is generated by ironsetup.py
__all__ = ["show"]
_config = {}
def show():
    print 'Numpy for IronPython'
""")
    fo.close()


def write_version(path):
    fo = open(path, 'w')
    fo.write("""# this file is generated by ironsetup.py
short_version = '2.0.0'
version = '2.0.0'
full_version= '2.0.0.dev-unknown'
git_revision = 'unknown'
release = False

if not release:
    version = full_version
""")
    fo.close()


if __name__ == '__main__':
    try:
        install()
    except (IOError, WindowsError) as ex:
        print "#"*80
        print
        print "you have no rights to write to: %s" % sys.prefix
        print "please, rerun iron_install.py in a shell with Administrator privilages"
        print
        print "#"*80
        print ex
