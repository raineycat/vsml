project('DotNetAppHost', 'cpp', version: '10.0.1')
cpp = meson.get_compiler('cpp')
lib_dir = join_paths(meson.current_source_dir(), 'runtimes', 'win-x64', 'native')

nethost_dep = declare_dependency(
  include_directories: include_directories('runtimes/win-x64/native'),
  dependencies: cpp.find_library(
    'libnethost',
    dirs: lib_dir,
    required: true,
  ),
)
