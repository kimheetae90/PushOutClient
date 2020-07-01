#!/bin/bash
 
echo "Uploading IPA to Appstore Connect..."
 
#Path is "$WORKSPACE/.build/last/<BUILD_TARGET_ID>/build.ipa"
path="$WORKSPACE/.build/last/qa_ios/build.ipa"
 
if xcrun altool --upload-app -f $path -u kimi10.develop@gmail.com -p $password ; then
    echo "Upload IPA to Appstore Connect finished with success"
else
    echo "Upload IPA to Appstore Connect failed"
fi