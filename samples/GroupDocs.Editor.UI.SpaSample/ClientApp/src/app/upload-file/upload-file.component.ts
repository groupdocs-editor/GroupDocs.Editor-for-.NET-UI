import { Component, OnInit } from '@angular/core';
import {StorageMetaFile, WordProcessingService} from "@groupdocs/groupdocs.editor.angular.ui-wordprocessing";

@Component({
  selector: 'app-upload-file',
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.css']
})
export class UploadFileComponent {
  file?: File;
  documentCode?: string;
  showLoading = false;
  allowAccept = false;
  constructor(private wordHttpService: WordProcessingService) { }
  onFileChange(event: any) {
    this.file = event.target.files[0]
  }

  upload() {
    if (this.file) {
      this.wordHttpService.wordProcessingUploadPost$Json({body:{
          File: this.file,
          "LoadOptions.Password": '',
          "EditOptions.EnablePagination": true,
          "EditOptions.EnableLanguageInformation": false,
          "EditOptions.ExtractOnlyUsedFont": false,
          "EditOptions.FontExtraction":0,
          "EditOptions.UseInlineStyles": false

        }}).subscribe({
        next: (data: StorageMetaFile) => {
          if (data) {
            this.documentCode = data.documentCode;
            this.showLoading = false;
            this.allowAccept = true;
          }
        },
        error: (error: any) => {console.error(error)},
      });
    } else {
      alert("Please select a file first")
    }
  }
  goToLink(): void {
    let url = `wordProcessing/${this.documentCode}`;
    window.open(url, '_blank');
  }
}
