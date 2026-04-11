cd D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Installer

wix build Package.wxs `
    -ext WixToolset.UI.wixext `
    -ext WixToolset.Util.wixext `
    -o FichaCostoService-Setup-v0.6.2.msi