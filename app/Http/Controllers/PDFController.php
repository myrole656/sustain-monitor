<?php

namespace App\Http\Controllers;

use Barryvdh\DomPDF\Facade\Pdf;
use App\Models\SDGStatus;
use App\Models\Project;
use Illuminate\Http\Request;

class PDFController extends Controller
{
    /**
     * Stream the project report PDF in the browser.
     */
    public function projectReport($id)
    {
        $project = Project::with(['process', 'user', 'sdgStatus'])->findOrFail($id);
        $process = $project->process;
        $sdgs = $project->sdgStatus;

        $completion = 0;
        if ($process) {
            $maxMarks = [
                'initiation'  => 17,
                'planning'    => 31,
                'execution'   => 60,
                'monitoring'  => 12,
                'closing'     => 14,
            ];
            $stepMarks = [
                'initiation'  => $process->initiation,
                'planning'    => $process->planning,
                'execution'   => $process->execution,
                'monitoring'  => $process->monitoring,
                'closing'     => $process->closing,
            ];
            $totalMax = array_sum($maxMarks);
            $totalUser = array_sum($stepMarks);
            $completion = $totalMax ? round(($totalUser / $totalMax) * 100, 1) : 0;
        }

        $pdf = Pdf::loadView('pdf.report', [
            'project'    => $project,
            'process'    => $process,
            'sdgs'       => $sdgs,
            'completion' => $completion,
        ])->setPaper('a4', 'portrait');

        return $pdf->stream("project-{$id}-report.pdf");
    }

    /**
     * Download the project report PDF.
     */
    public function downloadProjectReport($id)
    {
        $project = Project::with(['process', 'user', 'sdgStatus'])->findOrFail($id);
        $process = $project->process;
        $sdgs = $project->sdgStatus;

        $completion = 0;
        if ($process) {
            $maxMarks = [
                'initiation'  => 17,
                'planning'    => 31,
                'execution'   => 60,
                'monitoring'  => 12,
                'closing'     => 14,
            ];
            $stepMarks = [
                'initiation'  => $process->initiation,
                'planning'    => $process->planning,
                'execution'   => $process->execution,
                'monitoring'  => $process->monitoring,
                'closing'     => $process->closing,
            ];
            $totalMax = array_sum($maxMarks);
            $totalUser = array_sum($stepMarks);
            $completion = $totalMax ? round(($totalUser / $totalMax) * 100, 1) : 0;
        }

        $pdf = Pdf::loadView('pdf.report', [
            'project'    => $project,
            'process'    => $process,
            'sdgs'       => $sdgs,
            'completion' => $completion,
        ])->setPaper('a4', 'portrait');

        return $pdf->download("project-{$id}-report.pdf");
    }
}
