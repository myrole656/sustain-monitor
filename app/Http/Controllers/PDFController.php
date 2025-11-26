<?php

namespace App\Http\Controllers;

use Barryvdh\DomPDF\Facade\Pdf;
use App\Models\Project;
use Illuminate\Http\Request;

class PDFController extends Controller
{
    /**
     * Stream the project report PDF for in-browser preview.
     */
    public function projectReport($id)
    {
        // Load project with 'process' and 'user' relations
        $project = Project::with(['process', 'user'])->findOrFail($id);

        // Extract process (returns null if not exists)
        $process = $project->process;

        // Load the PDF view
        $pdf = Pdf::loadView('pdf.report', [
            'project' => $project,
            'process' => $process,
        ])->setPaper('a4', 'portrait');

        // Stream PDF in-browser
        return $pdf->stream("project-{$id}-report.pdf");
    }

    /**
     * Download the project report PDF.
     */
    public function downloadProjectReport($id)
    {
        $project = Project::with(['process', 'user'])->findOrFail($id);
        $process = $project->process;

        $pdf = Pdf::loadView('pdf.report', [
            'project' => $project,
            'process' => $process,
        ])->setPaper('a4', 'portrait');

        // Force download
        return $pdf->download("project-{$id}-report.pdf");
    }
}
